using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelService.Logging;
using ExcelService.Properties;
using ExcelService.Sessions;
using ExcelServiceModel;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace ExcelService.Hubs
{
    [HubName("excelHub")]
    [BasicAuthenticationAttribute]
    public class ExcelHub : Hub
    {
        private readonly IExcelSessionManager excelSessionManager;
        private readonly ILog log;
        public static readonly Dictionary<string,string> UserConnectionMapping = new Dictionary<string, string>(); 

        public ExcelHub(IExcelSessionManager excelSessionManager, ILog log)
        {
            if (excelSessionManager == null) throw new ArgumentException("excelSessionManager");
            if (log == null) throw new ArgumentException("log");
            this.excelSessionManager = excelSessionManager;
            this.log = log;
        }

        public bool IsConnectionInitialized()
        {
            log.Info("{0} - IsConnectionInitialized");
            if (UserConnectionMapping.ContainsKey(Context.User.Identity.Name))
            {
                return true;
            }

            return false;
        }

        public CellValue GetCellValue(CellValue cell)
        {
            log.Info("{0} - GetCellValue({1}!{2})", Context.User.Identity.Name, cell.Worksheet, cell.CellName);

            var session = excelSessionManager.GetSession(cell.SessionId);
            if (session == null) throw new Exception("Session not found");
            if (string.IsNullOrWhiteSpace(cell.CellName)) throw new ArgumentException();

            if (!session.Users.Contains(Context.User.Identity.Name)) throw new NotAuthorizedException();

            cell.Workbook = session.WorkbookName;
            cell.Value = session.GetCellValue(cell.Worksheet, cell.CellName);
            return cell;
        }

        public void PutCellValue(CellValue cell)
        {
            log.Info("{0} - PutCellValue({1}!{2})", Context.User.Identity.Name, cell.Worksheet, cell.CellName);
            var session = excelSessionManager.GetSession(cell.SessionId);

            if (session == null) throw new Exception("Session not found");
            if (!session.Users.Contains(Context.User.Identity.Name)) throw new NotAuthorizedException();

            session.UpdateCell(cell.Worksheet, cell.CellName, cell.Value);
        }

        public NameValue GetNameValue(NameValue name)
        {
            log.Info("{0} - GetNameValue({1})", Context.User.Identity.Name, name.Name);
            var session = excelSessionManager.GetSession(name.SessionId);
            if (session == null) throw new Exception("Session not found");
            if (!session.Users.Contains(Context.User.Identity.Name)) throw new NotAuthorizedException();

            name.Workbook = session.WorkbookName;
            name.Values = session.GetNameValue(name.Name);
            return name;
        }

        public void PutNameValue(NameValue name)
        {
            log.Info("{0} - PutNameValue({1})", Context.User.Identity.Name, name.Name);
            var session = excelSessionManager.GetSession(name.SessionId);
            if (session == null) throw new Exception("Session not found");
            if (!session.Users.Contains(Context.User.Identity.Name)) throw new NotAuthorizedException();

            session.UpdateName(name.Name, name.Values);
        }

        public override Task OnConnected()
        {
            var username = Context.Request.Headers[Settings.Default.UsernameHeader]??Context.Request.QueryString[Settings.Default.UsernameHeader];
            log.Debug("User {0} connected", username);

            if (UserConnectionMapping.ContainsKey(username))
            {
                UserConnectionMapping[username] = Context.ConnectionId;
            }
            else
            {
                UserConnectionMapping.Add(username, Context.ConnectionId);
            }

            var sessions = excelSessionManager.GetSessionsForUser(username);
            foreach (var excelSession in sessions)
            {
                Groups.Add(Context.ConnectionId, excelSession.Name);
            }
            return base.OnConnected();
        }

        public override Task OnDisconnected()
        {
            var username = Context.Request.Headers[Settings.Default.UsernameHeader]??Context.Request.QueryString[Settings.Default.UsernameHeader];
            UserConnectionMapping.Remove(username);
            log.Debug("User {0} disconnected", username);
            var sessions = excelSessionManager.GetSessionsForUser(username);
            foreach (var excelSession in sessions)
            {
                Groups.Remove(Context.ConnectionId, excelSession.Name);
            }
            return base.OnDisconnected();
        }
    }
}
