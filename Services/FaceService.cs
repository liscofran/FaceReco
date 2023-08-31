using FaceRecognition.Data;
using FaceRecognition.Models;
using MySqlConnector;


namespace FaceRecognition.Services
{
    public class FaceService
    {
        private readonly FaceContext _context = default!;
        private MySqlConnection conn;

        public FaceService(FaceContext context) 
        {
            _context = context;
            conn =  new MySqlConnection("server=127.0.0.1;port=3306;uid=FaceUser;pwd=Useruser!;database=facedb");
            conn.Open();
        }
        
        public IList<FaceUser> GetFaceUsers()
        {
            if(_context.FaceUsers != null)
            {
                List<FaceUser> list = new();

                using (conn)
                {
                    MySqlCommand cmd = new("select * from FaceUser", conn);

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        list.Add(new FaceUser()
                        {
                            Id = reader["Id"].ToString(),
                            Nome = reader["Nome"].ToString(),
                        });

                    }
                }
                return list;
            }
            return new List<FaceUser>();
        }

        public FaceUser GetFaceUser(string id)
        {
            

            if(_context.FaceUsers != null)
            {
                FaceUser user = new();

                using (conn)
                {
                    MySqlCommand cmd = new("select * from FaceUser where Id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    { 
                        user.Id = reader["Id"].ToString();
                        user.Nome = reader["Nome"].ToString();
                    }
                }

                return user;
            }

            return null;
        }

        public void AddFaceUsers(FaceUser FaceUser)
        {
            if (_context.FaceUsers != null)
            {
                using (conn)
                {
                    MySqlCommand cmd = new("insert into FaceUser (Id,Nome) VALUES (@id, @Nome)", conn);
                    cmd.Parameters.AddWithValue("@id", FaceUser.Id);
                    cmd.Parameters.AddWithValue("@Nome", FaceUser.Nome);
                    cmd.ExecuteNonQuery();
                }
                //_context.FaceUsers.Add(FaceUser);
                //_context.SaveChanges();
            }
        }

        public void DeleteFaceUsers(int id)
        {
            if (_context.FaceUsers != null)
            {
                //var FaceUsers = _context.FaceUsers.Find(id);
                //if (FaceUsers != null)
                //{
                    using (conn)
                    {
                        string query = "DELETE FROM FaceUser WHERE id = @id";
                        MySqlCommand cmd = new(query, conn);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                    // _context.FaceUsers.Remove(FaceUsers);
                    //_context.SaveChanges();
                //}
            }            
        } 
    }
}
