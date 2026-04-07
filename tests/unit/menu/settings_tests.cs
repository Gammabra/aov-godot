using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;

// =====================================================
// TESTS UNITAIRES - SettingsPages & SettingsSave
// =====================================================

[TestSuite]
public class SettingsPagesTests
{
	// =================================================
	// SUBTITLE VALUES
	// =================================================

	[TestCase]
	public void SubtitleDefaultValues_AreCorrect()
	{
		var page = new SettingsPages_mock();
		AssertThat(page.subtitles_enabled).IsFalse();
		AssertThat(page.subtitle_size).IsEqual(30.0f);
		AssertThat(page.subtitle_font_index).IsEqual(0);
		AssertThat(page.subtitle_text_color).IsEqual(0);
		AssertThat(page.subtitle_bg_color).IsEqual(0);
		AssertThat(page.subtitle_language).IsEqual(0);
		AssertThat(page.subtitle_opacity).IsEqual(50.0f);
	}

	[TestCase]
	public void SubtitleToggle_UpdatesValue()
	{
		var page = new SettingsPages_mock();
		page.subtitles_enabled = true;
		AssertThat(page.subtitles_enabled).IsTrue();
		page.subtitles_enabled = false;
		AssertThat(page.subtitles_enabled).IsFalse();
	}

	[TestCase]
	public void SubtitleSize_UpdatesCorrectly()
	{
		var page = new SettingsPages_mock();
		page.subtitle_size = 10.0f;
		AssertThat(page.subtitle_size).IsEqual(10.0f);
		page.subtitle_size = 100.0f;
		AssertThat(page.subtitle_size).IsEqual(100.0f);
	}

	[TestCase]
	public void SubtitleOpacity_NormalizesToAlpha()
	{
		var page = new SettingsPages_mock();
		page.subtitle_opacity = 75.0f;
		float alpha = page.subtitle_opacity / 100.0f;
		AssertThat(alpha).IsEqual(0.75f);
	}

	[TestCase]
	public void SubtitleOpacity_0_GivesAlpha0()
	{
		var page = new SettingsPages_mock();
		page.subtitle_opacity = 0.0f;
		float alpha = page.subtitle_opacity / 100.0f;
		AssertThat(alpha).IsEqual(0.0f);
	}

	[TestCase]
	public void SubtitleOpacity_100_GivesAlpha1()
	{
		var page = new SettingsPages_mock();
		page.subtitle_opacity = 100.0f;
		float alpha = page.subtitle_opacity / 100.0f;
		AssertThat(alpha).IsEqual(1.0f);
	}

	[TestCase]
	public void SubtitleLanguage_0_IsEnglish()
	{
		var page = new SettingsPages_mock();
		page.subtitle_language = 0;
		AssertThat(page.GetPreviewText()).IsEqual("Subtitle");
	}

	[TestCase]
	public void SubtitleLanguage_1_IsFrench()
	{
		var page = new SettingsPages_mock();
		page.subtitle_language = 1;
		AssertThat(page.GetPreviewText()).IsEqual("Sous-Titre");
	}

	[TestCase]
	public void SubtitleLanguage_OutOfBounds_FallsBackToEnglish()
	{
		var page = new SettingsPages_mock();
		page.subtitle_language = 99;
		AssertThat(page.GetPreviewText()).IsEqual("Subtitle");
	}

	[TestCase]
	public void SubtitleBgColor_Index4_IsTransparent()
	{
		var page = new SettingsPages_mock();
		page.subtitle_bg_color = 4;
		AssertThat(page.IsBgTransparent()).IsTrue();
	}

	[TestCase]
	public void SubtitleBgColor_Index0_IsNotTransparent()
	{
		var page = new SettingsPages_mock();
		page.subtitle_bg_color = 0;
		AssertThat(page.IsBgTransparent()).IsFalse();
	}

	[TestCase]
	public void SubtitleBgColor_AllValidIndexes_AreNotTransparent()
	{
		var page = new SettingsPages_mock();
		for (int i = 0; i < 4; i++)
		{
			page.subtitle_bg_color = i;
			AssertThat(page.IsBgTransparent()).IsFalse();
		}
	}

	// =================================================
	// VIDEO VALUES
	// =================================================

	[TestCase]
	public void VideoDefaultValues_AreCorrect()
	{
		var page = new SettingsPages_mock();
		AssertThat(page.contrast).IsEqual(50.0f);
		AssertThat(page.brightness).IsEqual(50.0f);
		AssertThat(page.animations_enabled).IsFalse();
		AssertThat(page.resolution_index).IsEqual(0);
		AssertThat(page.window_mode).IsEqual(0);
		AssertThat(page.texture_quality).IsEqual(0);
	}

	[TestCase]
	public void Resolution_Index0_Is1920x1080()
	{
		var page = new SettingsPages_mock();
		var res = page.GetResolutionForIndex(0);
		AssertThat(res.X).IsEqual(1920);
		AssertThat(res.Y).IsEqual(1080);
	}

	[TestCase]
	public void Resolution_Index1_Is1280x720()
	{
		var page = new SettingsPages_mock();
		var res = page.GetResolutionForIndex(1);
		AssertThat(res.X).IsEqual(1280);
		AssertThat(res.Y).IsEqual(720);
	}

	[TestCase]
	public void Resolution_UnknownIndex_FallsBackTo1920x1080()
	{
		var page = new SettingsPages_mock();
		var res = page.GetResolutionForIndex(99);
		AssertThat(res.X).IsEqual(1920);
		AssertThat(res.Y).IsEqual(1080);
	}

	[TestCase]
	public void WindowMode_Default_IsWindowed()
	{
		var page = new SettingsPages_mock();
		AssertThat(page.window_mode).IsEqual(0);
	}

	[TestCase]
	public void WindowMode_1_IsFullscreen()
	{
		var page = new SettingsPages_mock();
		page.window_mode = 1;
		AssertThat(page.window_mode).IsEqual(1);
	}

	// =================================================
	// VISUAL VALUES
	// =================================================

	[TestCase]
	public void VisualDefaultValues_AreCorrect()
	{
		var page = new SettingsPages_mock();
		AssertThat(page.interface_size).IsEqual(1.0f);
		AssertThat(page.blurry_enabled).IsFalse();
		AssertThat(page.camera_shake_enabled).IsFalse();
		AssertThat(page.visual_indicators_enabled).IsFalse();
		AssertThat(page.color_blindness).IsEqual(0);
	}

	// =================================================
	// AUDIO VALUES
	// =================================================

	[TestCase]
	public void AudioDefaultValues_AreCorrect()
	{
		var page = new SettingsPages_mock();
		AssertThat(page.master_volume).IsEqual(50.0f);
		AssertThat(page.music_volume).IsEqual(50.0f);
		AssertThat(page.voices_volume).IsEqual(50.0f);
		AssertThat(page.sfx_volume).IsEqual(50.0f);
	}

	[TestCase]
	public void AudioVolume_UpdatesCorrectly()
	{
		var page = new SettingsPages_mock();
		page.master_volume = 75.0f;
		AssertThat(page.master_volume).IsEqual(75.0f);
		page.sfx_volume = 0.0f;
		AssertThat(page.sfx_volume).IsEqual(0.0f);
	}

	// =================================================
	// ACTIONS
	// =================================================

	[RequireGodotRuntime]
	[TestCase]
	public void Actions_ContainsAllExpectedKeys()
	{
		var page = new SettingsPages_mock();
		string[] expected = {
			"move_up", "move_down", "move_left", "move_right",
			"battle_move_unit_to",
			"battle_select_skill1", "battle_select_skill2", "battle_select_skill3",
			"battle_select_skill4", "battle_select_skill5",
			"battle_pass_turn", "toggle_options"
		};

		foreach (var key in expected)
			AssertThat(page.actions.ContainsKey(key)).IsTrue();
	}

	[RequireGodotRuntime]
	[TestCase]
	public void Actions_Count_Is12()
	{
		var page = new SettingsPages_mock();
		AssertThat(page.actions.Count).IsEqual(12);
	}
}

// =====================================================
// MOCK - Reproduit la logique pure sans SceneTree
// =====================================================
public class SettingsPages_mock
{
	// SUBTITLE
	public bool subtitles_enabled = false;
	public float subtitle_size = 30.0f;
	public int subtitle_font_index = 0;
	public int subtitle_text_color = 0;
	public int subtitle_bg_color = 0;
	public int subtitle_language = 0;
	public float subtitle_opacity = 50.0f;

	// VIDEO
	public float contrast = 50.0f;
	public float brightness = 50.0f;
	public bool animations_enabled = false;
	public int resolution_index = 0;
	public int window_mode = 0;
	public int texture_quality = 0;

	// VISUAL
	public float interface_size = 1.0f;
	public bool blurry_enabled = false;
	public bool camera_shake_enabled = false;
	public bool visual_indicators_enabled = false;
	public int color_blindness = 0; // 0 = none, 1 = protanopia, 2 = deuteranopia, 3 = tritanopia, 4 = achromatopsia

	// AUDIO
	public float master_volume = 50.0f;
	public float music_volume = 50.0f;
	public float voices_volume = 50.0f;
	public float sfx_volume = 50.0f;

	// ACTIONS
	public Dictionary<string, string> actions = new()
	{
		{ "move_up",              "move_up" },
		{ "move_down",            "move_down" },
		{ "move_left",            "move_left" },
		{ "move_right",           "move_right" },
		{ "battle_move_unit_to",  "battle_move_unit_to" },
		{ "battle_select_skill1", "battle_select_skill1" },
		{ "battle_select_skill2", "battle_select_skill2" },
		{ "battle_select_skill3", "battle_select_skill3" },
		{ "battle_select_skill4", "battle_select_skill4" },
		{ "battle_select_skill5", "battle_select_skill5" },
		{ "battle_pass_turn",     "battle_pass_turn" },
		{ "toggle_options",       "toggle_options" }
	};

	public string GetPreviewText()
	{
		string[] previewTexts = { "Subtitle", "Sous-Titre" };
		return subtitle_language < previewTexts.Length
			? previewTexts[subtitle_language]
			: "Subtitle";
	}

	public bool IsBgTransparent() => subtitle_bg_color == 4;

	public (int X, int Y) GetResolutionForIndex(int index)
	{
		return index switch
		{
			0 => (1920, 1080),
			1 => (1280, 720),
			_ => (1920, 1080)
		};
	}
}
