using System.Collections.Generic;
using Platform.DTOs;

namespace Platform.Interfaces
{
    public interface ISearchable
    {
        string SearchKey { get; set; }

        List<string> Tags { get; set; }

        bool MatchSearch(string query);

        SearchItem GetSearchResultData();

        int GetSearchWeight(string query);
    }
}