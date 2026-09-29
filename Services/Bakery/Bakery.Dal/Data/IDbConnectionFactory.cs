using System.Data;

namespace Bakery.DAL.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
