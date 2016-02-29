using System;
using System.Collections.Generic;
using System.Linq;
using ExcelService.Logging;
using ExcelServiceModel;
using Microsoft.AspNet.SignalR;

namespace ExcelService.Sessions
{
    public class ExcelSessionManager : IExcelSessionManager
    {
        private readonly ILog log;
        private readonly IHubContext hubContext;
        private readonly List<ExcelWorkbookSession> sessions;
        
        public ExcelSessionManager(ILog log, IHubContext hubContext)
        {
            if (log == null) throw new ArgumentException("log");
            if (hubContext == null) throw new ArgumentException("hubContext");

            this.log = log;
            this.hubContext = hubContext;
            sessions = new List<ExcelWorkbookSession>();
        }


        public IEnumerable<ExcelSession> GetSessionsForUser(string username)
        {
            return sessions.Where(s => s.Users.Contains(username));
        }

        public IEnumerable<ExcelSession> GetAllSessions()
        {
            return sessions;
        }

        public ExcelWorkbookSession GetSession(int id)
        {
            var mySession = sessions.FirstOrDefault(s => s.Id == id);
            return mySession;
        }

        public void AddUserToSession(int id, string username)
        {
            var session = GetSession(id);
            if (session == null) return;

            if (!session.Users.Contains(username))
            {
                session.Users.Add(username);
            }
        }

        public void RemoveUserFromSession(int id, string username)
        {
            var session = GetSession(id);
            if (session == null) return;

            if (!session.Users.Contains(username))
            {
                session.Users.Remove(username);
            }
        }

        public ExcelWorkbookSession GetSession(string sessionName)
        {
            var mySession = sessions.FirstOrDefault(s => s.Name == sessionName);
            return mySession;
        }

        public ExcelWorkbookSession CreateSession(ExcelSession session)
        {
            var existingSession = GetSession(session.Name);

            if (existingSession != null) throw new Exception(string.Format("Session with name {0} already exists, session names should be unique", session.Name));

            var mySession = new ExcelWorkbookSession(session);
            mySession.CellValueChanged += MySessionOnCellValueChanged;
            mySession.NameValueChanged += MySessionOnNameValueChanged;
            mySession.Id = sessions.Any() ? sessions.Max(s => s.Id) + 1 : 0;
            mySession.Created = DateTime.Now;
            mySession.Users = new List<string> { session.Owner };

            foreach (var watchCell in mySession.WatchCells)
            {
                string worksheet,cellname;
                if (watchCell.Contains('!'))
                {
                    var cellParts = watchCell.Split('!');
                    worksheet = cellParts[0];
                    cellname = cellParts[1];
                }
                else
                {
                    worksheet = mySession.Workbook.Worksheets[0].Name;
                    cellname = watchCell;
                }
                
                mySession.WatchCell(worksheet, cellname);
            }

            foreach (var watchName in mySession.WatchNames)
            {
                mySession.WatchName(watchName);
            }

            sessions.Add(mySession);

            return mySession;
        }

        private void MySessionOnNameValueChanged(object sender, NameCache nameCache)
        {
            var session = (ExcelWorkbookSession) sender;
            var cacheValues = (object[]) nameCache.Value;
            if (log.IsDebugEnabled)
            {
                log.Debug("Send Name update to group {0}: {1} {2}", session.Name, nameCache.Name, string.Join(", ", cacheValues));
            }
            hubContext.Clients.Group(session.Name).NameValueUpdated(new NameValue(session.Id, session.WorkbookName, nameCache.Name, cacheValues));
        }

        private void MySessionOnCellValueChanged(object sender, CellCache cellCache)
        {
            var session = (ExcelWorkbookSession) sender;
            hubContext.Clients.Group(session.Name).CellValueUpdated(new CellValue(session.Id, session.WorkbookName, cellCache.Worksheet, cellCache.CellName, cellCache.Value));
        }

        public ExcelSession UpdateSession(int id, ExcelSession session)
        {
            var mySession = sessions.FirstOrDefault(s => s.Id == id);
            if (mySession == null)
            {
                return null;
            }

            mySession.Name = session.Name;
            mySession.Owner = session.Owner;
            mySession.Users = session.Users;

            return mySession;
        }

        public void DeleteSession(int id)
        {
            var session = GetSession(id);
            session.NameValueChanged -= MySessionOnNameValueChanged;
            session.CellValueChanged -= MySessionOnCellValueChanged;

            sessions.Remove(session);
        }

        public void InitializeSession(int id)
        {
            
        }
    }
}
