namespace BasketWorld.ViewModels
{
    public class StandingRowVM
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = "";
        public string? LogoUrl { get; set; }

        public int Played { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }

        public int PointsFor { get; set; }
        public int PointsAgainst { get; set; }

        public int Diff => PointsFor - PointsAgainst;
        public double WinPct => Played == 0 ? 0 : (double)Wins / Played;
    }
}
