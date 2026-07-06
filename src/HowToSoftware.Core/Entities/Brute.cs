namespace HowToSoftware.Core.Entities;

public class Brute
{
    public string Key { get; set; } = null!;
    public long FirstRequest { get; set; }
    public long LastRequest { get; set; }
    public long Lifetime { get; set; }
    public int Count { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
