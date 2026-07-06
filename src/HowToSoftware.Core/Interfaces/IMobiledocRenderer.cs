namespace HowToSoftware.Core.Interfaces;

/// <summary>
/// Renders Mobiledoc JSON to HTML (legacy format — Ghost 0.3.1/4.0).
/// </summary>
public interface IMobiledocRenderer
{
    string Render(string mobiledocJson);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
