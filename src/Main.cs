// Copyright (c) 2017 Leacme (http://leac.me). View LICENSE.md for more information.
using Godot;
using System;
using System.Collections.Generic;

public class Main : Spatial {

	public AudioStreamPlayer Audio { get; } = new AudioStreamPlayer();

	private AudioEffectSpectrumAnalyzerInstance spectrumEffect;
	private AudioEffectRecord recordEffect;

	private void InitSound() {
		if (!Lib.Node.SoundEnabled) {
			AudioServer.SetBusMute(AudioServer.GetBusIndex("Master"), true);
		}
	}

	public override void _Notification(int what) {
		if (what is MainLoop.NotificationWmGoBackRequest) {
			GetTree().ChangeScene("res://scenes/Menu.tscn");
		}
	}

	public override void _EnterTree() {
		var masterIndex = AudioServer.GetBusIndex("Master");
		AudioServer.AddBus(masterIndex + 1);
		AudioServer.SetBusName(masterIndex + 1, "Record");
		AudioServer.AddBusEffect(masterIndex + 1, new AudioEffectSpectrumAnalyzer(), 0);
		AudioServer.AddBusEffect(masterIndex + 1, new AudioEffectRecord(), 1);
		AudioServer.SetBusMute(masterIndex + 1, true);

	}

	public override void _Ready() {
		GetNode<WorldEnvironment>("sky").Environment.BackgroundColor = new Color(Lib.Node.BackgroundColorHtmlCode);
		GetNode<WorldEnvironment>("sky").Environment.GlowEnabled = true;
		GetNode<WorldEnvironment>("sky").Environment.GlowIntensity = 0.8f;
		GetNode<WorldEnvironment>("sky").Environment.GlowStrength = 1f;
		GetNode<WorldEnvironment>("sky").Environment.GlowBicubicUpscale = true;
		InitSound();
		AddChild(Audio);

		spectrumEffect = (AudioEffectSpectrumAnalyzerInstance)AudioServer.GetBusEffectInstance(AudioServer.GetBusIndex("Record"), 0);
		recordEffect = (AudioEffectRecord)AudioServer.GetBusEffect(AudioServer.GetBusIndex("Record"), 1);
		var recordNode = new AudioStreamPlayer() { Autoplay = true, Stream = new AudioStreamMicrophone(), Bus = "Record" };
		AddChild(recordNode);

		for (int i = 0; i < 16; i++) {
			var ring = GetNode("Woofer").GetNode<MeshInstance>("Ring" + i);
			ring.MaterialOverride = new SpatialMaterial() {
				AlbedoColor = Color.FromHsv(i * 22f / 360, 1, 1),
				EmissionEnabled = true,
				Emission = Color.FromHsv(i * 22f / 360, 1, 1)
			};
		}
	}

	public override void _Process(float delta) {
		float maxFreq = 11050;
		int minDbl = 60;
		int segs = 16;
		float prevHz = 0;
		for (int i = 0; i < segs; i++) {
			var hz = i * maxFreq / segs;
			var mag = spectrumEffect.GetMagnitudeForFrequencyRange(prevHz, hz);
			var nrg = Mathf.Clamp((minDbl + GD.Linear2Db(mag.Length())) / minDbl, 0, 1);
			var ring = GetNode("Woofer").GetNode<MeshInstance>("Ring" + i);
			ring.Scale = new Vector3(ring.Scale.x, nrg * 20 + 1, ring.Scale.z);
			((SpatialMaterial)ring.MaterialOverride).EmissionEnergy = nrg;
			prevHz = hz;
		}
	}
}
