using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaServer.DAL;
using MediaServer.Models;
using System.Data.SqlClient;

namespace MediaServer.UnitTests
{
    
    [TestFixture]
    public class FileContextTests
    {
        [Test]
        public void CreateFileRecord()
        {
            UserContext _userContext = new UserContext();
            CUser user = _userContext.GetByName("Dmitrii");
            FileContext _fileContext = new FileContext();
            byte[] hash = Encoding.ASCII.GetBytes("0eff316809032f72e0237f8bcb1b65cb");

            SqlConnection myConnection = new SqlConnection();
            myConnection.ConnectionString = ConnectionContext.GetConnectionString();
            myConnection.Open();
            SqlTransaction myTransaction = myConnection.BeginTransaction("testTransaction");

  
            CFile cFile = new CFile(Guid.NewGuid(), "testFile", "some/weird/path", 1024, user.Guid, true, hash, DateTime.Now, 0, 0, 0);
            int created = _fileContext.Create(cFile, myConnection, myTransaction);
            Assert.AreEqual(created, 1);
        }
    }
}
