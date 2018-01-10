using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rv
{
    public class AVMuteCommand : Command
    {
        private AVMuteCommand.Action _action = Action.UNKNOWN;
        private Action _status;

        public AVMuteCommand(AVMuteCommand.Action action)
        {
            _action = action;
        }

        public enum Action
        {
            QUERYSTATE,
            MUTE=31,
            UNMUTE=30,
            UNKNOWN
        }
        internal override string getCommandString()
        {
            string command;
            switch (_action)
            {
                case Action.QUERYSTATE:
                    command = "%1AVMT ?";
                    break;
                case Action.MUTE:
                    command = "%1AVMT 31";
                    break;
                case Action.UNMUTE:
                    command = "%1AVMT 30";
                    break;
                default:
                    command = "";
                    break;
            }
            return command;

        }

        internal override bool processAnswerString(string a)
        {
            if (!base.processAnswerString(a))
            {
                _status = Action.UNKNOWN;
                return false;
            }

            if (_action == Action.QUERYSTATE)
            {
                a = a.Replace("%1AVMT=", "");
                int retVal = int.Parse(a);
                if (retVal >= (int)Action.UNMUTE && retVal <= (int)Action.MUTE)
                    _status = (Action)(retVal);
                else
                    _status = Action.UNKNOWN;
            }

            return true;
        }

        public Action Status
        {
            get { return _status; }
        } 
    }
}
