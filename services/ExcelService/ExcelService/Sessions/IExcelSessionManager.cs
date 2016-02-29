using System.Collections.Generic;
using ExcelServiceModel;

namespace ExcelService.Sessions
{
    public interface IExcelSessionManager
    {
        IEnumerable<ExcelSession> GetSessionsForUser(string username);
        IEnumerable<ExcelSession> GetAllSessions();
        void AddUserToSession(int id, string username);
        void RemoveUserFromSession(int id, string username);
        ExcelWorkbookSession GetSession(int id);
        ExcelWorkbookSession GetSession(string sessionName);
        ExcelWorkbookSession CreateSession(ExcelSession session);
        ExcelSession UpdateSession(int id, ExcelSession session);
        void DeleteSession(int id);
        void InitializeSession(int id);
    }
}