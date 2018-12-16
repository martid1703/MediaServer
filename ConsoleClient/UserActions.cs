using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleClient
{
    /// <summary>
    /// Keeps a queue of actions started but not finished, like file uploading, so they can be continued after restart of the program
    /// todo: save/load user actions to/from "UserActions.xml"
    /// </summary>
    static class UserActions
    {
        static public Queue<Action> Actions { get; set; }

    }
}
