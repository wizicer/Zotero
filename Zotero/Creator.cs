using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Zotero
{
    public enum CreatorType
    {
        Author = 1,
        Contributor = 2,
        Editor = 3,
        Translator = 4,
        SeriesEditor = 5,
        Interviewee = 6,
        Interviewer = 7,
        Director = 8,
        Scriptwriter = 9,
        Producer = 10,
        CastMember = 11,
        Sponsor = 12,
        Counsel = 13,
        Inventor = 14,
        AttorneyAgent = 15,
        Recipient = 16,
        Performer = 17,
        Composer = 18,
        WordsBy = 19,
        Cartographer = 20,
        Programmer = 21,
        Artist = 22,
        Commenter = 23,
        Presenter = 24,
        Guest = 25,
        Podcaster = 26,
        ReviewedAuthor = 27,
        Cosponsor = 28,
        BookAuthor = 29
    }
    public class Creator : ZoteroObject
    {
        public Creator(CreatorType type, string firstName, string lastName)
        {
            this.Type = type;
            this.FirstName = firstName;
            this.LastName = lastName;
        }

        public CreatorType Type { get; init; }
        public string FirstName { get; init; }
        public string LastName { get; init; }
    }
}
