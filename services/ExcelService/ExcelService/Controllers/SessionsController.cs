using System;
using System.Linq;
using System.Web.Http;
using ExcelService.Hubs;
using ExcelService.Logging;
using ExcelService.Sessions;
using ExcelServiceModel;
using Microsoft.AspNet.SignalR;

namespace ExcelService.Controllers
{
    [RoutePrefix("excel/sessions")]
    public class SessionsController : ApiController
    {
        private readonly IExcelSessionManager excelSessionManager;
        private readonly ILog log;
        private readonly IHubContext hubContext;

        public SessionsController(IExcelSessionManager excelSessionManager, ILog log, IHubContext hubContext)
        {
            if (excelSessionManager == null) throw new ArgumentException("excelSessionManager");
            if (log == null) throw new ArgumentException("log");
            if (hubContext == null) throw new ArgumentException("hubContext");

            this.excelSessionManager = excelSessionManager;
            this.log = log;
            this.hubContext = hubContext;
        }

        // GET excel/sessions
        [BasicAuthentication]
        public IHttpActionResult GetAllSessions()
        {
            log.Info("{0} - GetAllSessions", User.Identity.Name);
            var sessions = excelSessionManager.GetAllSessions().Select(s=>new ExcelSession(s)).ToList();
            return Ok(sessions);
        }

        // GET excel/mySessions
        [BasicAuthentication]
        [Route("mine")]
        public IHttpActionResult GetMySessions()
        {
            log.Info("{0} - GetMySessions", User.Identity.Name);
            var sessions = excelSessionManager.GetSessionsForUser(User.Identity.Name).Select(s => new ExcelSession(s)).ToList();
            return Ok(sessions);
        }

        // GET excel/sessions/{id}
        [BasicAuthentication]
        public IHttpActionResult GetSession(int id)
        {
            log.Info("{0} - GetSession({1})", User.Identity.Name, id);
            var session = excelSessionManager.GetSession(id);

            if (session == null)
            {
                return NotFound();
            }
            
            if (!session.Users.Contains(User.Identity.Name))
            {
                return Unauthorized();
            }

            // Re-notify all values to client
            var myConnection = ExcelHub.UserConnectionMapping[User.Identity.Name];
            foreach (var watchedItem in session.WatchedItems)
            {
                if (watchedItem is CellCache)
                {
                    var cellCache = (CellCache)watchedItem;
                    hubContext.Clients.Client(myConnection).CellValueUpdated(new CellValue(session.Id, cellCache.Workbook, cellCache.Worksheet, cellCache.CellName, cellCache.Value));
                }
                else if (watchedItem is NameCache)
                {
                    var nameCache = (NameCache)watchedItem;
                    hubContext.Clients.Client(myConnection).NameValueUpdated(new NameValue(session.Id, nameCache.Workbook, nameCache.Name, (object[])nameCache.Value));
                }
            }

            return Ok(session);
        }

        // GET excel/sessions/{id}/join
        [BasicAuthentication]
        [HttpGet]
        [Route("{id}/join")]
        public IHttpActionResult JoinSession(int id)
        {
            log.Info("{0} - JoinSession({1})", User.Identity.Name, id);

            if (!ExcelHub.UserConnectionMapping.ContainsKey(User.Identity.Name)) return NotFound();


            var session = excelSessionManager.GetSession(id);
            if (session == null) return NotFound();
            log.Debug("User {0} joins session {1}({2})", User.Identity.Name, session.Name, session.Id);

            excelSessionManager.AddUserToSession(id, User.Identity.Name);

            var myConnection = ExcelHub.UserConnectionMapping[User.Identity.Name];

            log.Debug("Add connection {0} to group {1}", myConnection, session.Name);
            hubContext.Groups.Add(myConnection, session.Name);

            foreach (var watchedItem in session.WatchedItems)
            {
                if (watchedItem is CellCache)
                {
                    var cellCache = (CellCache)watchedItem;
                    hubContext.Clients.Client(myConnection).CellValueUpdated(new CellValue(session.Id, cellCache.Workbook, cellCache.Worksheet, cellCache.CellName, cellCache.Value));
                }
                else if (watchedItem is NameCache)
                {
                    var nameCache = (NameCache)watchedItem;
                    hubContext.Clients.Client(myConnection).NameValueUpdated(new NameValue(session.Id, nameCache.Workbook, nameCache.Name, (object[])nameCache.Value));
                }
            }

            return Ok();
        }

        // GET excel/sessions/{id}/leave
        [BasicAuthentication]
        public IHttpActionResult LeaveSession(int id)
        {
            log.Info("{0} - LeaveSession({1})", User.Identity.Name, id);
            var session = excelSessionManager.GetSession(id);
            if (session == null) return NotFound();

            excelSessionManager.RemoveUserFromSession(id, User.Identity.Name);
            session = excelSessionManager.GetSession(id);
            if (session.Users.Count == 0)
            {
                excelSessionManager.DeleteSession(session.Id);
            }

            var myConnection = ExcelHub.UserConnectionMapping[User.Identity.Name];

            log.Debug("Remove connection {0} from group {1}", myConnection, session.Name);
            hubContext.Groups.Remove(myConnection, session.Name);

            return Ok();   
        }

        // POST excel/sessions
        [BasicAuthentication]
        public IHttpActionResult CreateSession(ExcelSession session)
        {
            log.Info("{0} - CreateSession", User.Identity.Name);

            if (string.IsNullOrWhiteSpace(session.Name)) return InternalServerError();
            if (string.IsNullOrWhiteSpace(session.WorkbookName)) return InternalServerError();

            session.Owner = User.Identity.Name;

            ExcelWorkbookSession createdSession = null;
            try
            {
                createdSession = excelSessionManager.CreateSession(session);
            }
            catch (Exception ex)
            {
                log.Error("Could not create excel session: {0}", ex.Message);
                return InternalServerError(ex);
            }

            var myConnection = ExcelHub.UserConnectionMapping[User.Identity.Name];

            log.Debug("Add connection {0} to group {1}", myConnection, session.Name);
            hubContext.Groups.Add(myConnection, session.Name);

            foreach (var watchedItem in createdSession.WatchedItems)
            {
                if (watchedItem is CellCache)
                {
                    var cellCache = (CellCache) watchedItem;
                    hubContext.Clients.Client(myConnection).CellValueUpdated(new CellValue(createdSession.Id, cellCache.Workbook, cellCache.Worksheet, cellCache.CellName, cellCache.Value));
                }
                else if (watchedItem is NameCache)
                {
                    var nameCache = (NameCache)watchedItem;
                    hubContext.Clients.Client(myConnection).NameValueUpdated(new NameValue(createdSession.Id, nameCache.Workbook, nameCache.Name, (object[])nameCache.Value));
                }

            }

            return Ok(session);
        }

        // PUT excel/sessions
        [BasicAuthentication]
        public IHttpActionResult UpdateSession(int id, ExcelSession session)
        {
            log.Info("{0} - UpdateSession({1})", User.Identity.Name, id);
            ExcelSession mySession = excelSessionManager.GetSession(id);
            if (mySession.Owner != User.Identity.Name)
            {
                return Unauthorized();
            }

            mySession = excelSessionManager.UpdateSession(id, session);

            return Ok(mySession);
        }

        // DELETE excel/sessions/{id}
        [BasicAuthentication]
        public IHttpActionResult DeleteSession(int id)
        {
            log.Info("{0} - DeleteSession({1})", User.Identity.Name, id);
            var mySession = excelSessionManager.GetSession(id);
            if (mySession == null)
            {
                return NotFound();
            }

            if (mySession.Owner != User.Identity.Name)
            {
                return Unauthorized();
            }

            excelSessionManager.DeleteSession(id);
            return Ok();
        }

        // GET excel/sessions/{id}/worksheets
        [BasicAuthentication]
        [HttpGet]
        [Route("{id}/worksheets")]
        public IHttpActionResult GetWorksheets(int id)
        {
            log.Info("{0} - GetWorksheets", User.Identity.Name);
            var session = excelSessionManager.GetSession(id);
            if (session == null) return NotFound();

            if (!session.Users.Contains(User.Identity.Name)) return Unauthorized();

            var worksheets = session.Workbook.Worksheets.Select(w => w.Name);

            return Ok(worksheets);
        }

        // GET excel/sessions/{id}/names
        [BasicAuthentication]
        [HttpGet]
        [Route("{id}/names")]
        public IHttpActionResult GetNames(int id)
        {
            log.Info("{0} - GetNames", User.Identity.Name);
            var session = excelSessionManager.GetSession(id);
            if (session == null) return NotFound();

            if (!session.Users.Contains(User.Identity.Name)) return Unauthorized();

            var names = session.Workbook.Worksheets.Names.Select(n => n.Text);

            return Ok(names);
        }

        // GET excel/sessions/{id}/{worksheet}/{cellName}
        [BasicAuthentication]
        [HttpGet]
        [Route("{id}/{worksheet}/{cellName}")]
        public IHttpActionResult GetCellValue(int id, string worksheet, string cellName)
        {
            log.Info("{0} - GetCellValue({1}!{2})", User.Identity.Name, worksheet, cellName);
            var session = excelSessionManager.GetSession(id);
            if (session == null) return NotFound();

            if (!session.Users.Contains(User.Identity.Name)) return Unauthorized();
            if (session.Workbook.Worksheets.All(ws => ws.Name != worksheet)) return NotFound();

            var cellValue = session.GetCellValue(worksheet, cellName);

            return Ok(cellValue);
        }

        // PUT excel/sessions/{id}/{worksheet}/{cellName}
        [BasicAuthentication]
        [HttpPut]
        [Route("{id}/{worksheet}/{cellName}")]
        public IHttpActionResult PutCellValue(int id, string worksheet, string cellName, [FromBody]object value)
        {
            log.Info("{0} - PutCellValue({1}!{2})", User.Identity.Name, worksheet, cellName);
            var session = excelSessionManager.GetSession(id);
            if (session == null) return NotFound();
            if (!session.Users.Contains(User.Identity.Name)) return Unauthorized();
            if (session.Workbook.Worksheets.All(ws => ws.Name != worksheet)) return NotFound();

            session.UpdateCell(worksheet, cellName, value);
            return Ok();
        }

        // GET excel/sessions/{id}/names/{name}
        [BasicAuthentication]
        [HttpGet]
        [Route("{id}/names/{name}")]
        public IHttpActionResult GetNameValue(int id, string name)
        {
            log.Info("{0} - GetNameValue({1})", User.Identity.Name, name);
            var session = excelSessionManager.GetSession(id);
            if (session == null) return NotFound();

            if (!session.Users.Contains(User.Identity.Name)) return Unauthorized();

            var nameValue = session.GetNameValue(name);

            return Ok(nameValue);
        }

        // PUT excel/sessions/{id}/names/{name}
        [BasicAuthentication]
        [HttpPut]
        [Route("{id}/names/{name}")]
        public IHttpActionResult PutNameValue(int id, string name, [FromBody]object[] value)
        {
            log.Info("{0} - PutNameValue({1})", User.Identity.Name, name);
            var session = excelSessionManager.GetSession(id);
            if (session == null) return NotFound();

            if (!session.Users.Contains(User.Identity.Name)) return Unauthorized();

            session.UpdateName(name, value);

            return Ok();
        }
    }
}
