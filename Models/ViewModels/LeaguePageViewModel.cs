using System;
using System.Collections.Generic;
using BasketWorld.Data; // <- tes entités (Team, Game)

namespace BasketWorld.Models.ViewModels
{
    public class LeaguePageViewModel
    {
        public string LeagueName { get; set; } = "";
        public IList<Team> Teams { get; set; } = new List<Team>();
        public IList<Game> UpcomingGames { get; set; } = new List<Game>();
        public IList<Game> PastGames { get; set; } = new List<Game>();
    }
}
