using System;

namespace csImb
{
    public class Command : Message
    {
        

        private string _commandName;

        public string CommandName
        {
            get { return _commandName; }
            set { _commandName = value; }
        }

        private string _data;

        public string Data
        {
            get { return _data; }
            set { _data = value; }
        }
        

        public override string ToString()
        {
            return SenderId + "|" + SenderName + "|" + CommandName + "|" + Data;
        }

        public static Command FromString(string value)
        {
            try
            {
                var s = value.Split('|');
                var result = new Command();
                result.SenderId = int.Parse(s[0]);
                result.SenderName = s[1];
                result.CommandName = s[2];
                result.Data = s[3];
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error parsing command:" + e.Message);
                return null;
            }
        }
        
    }
}
