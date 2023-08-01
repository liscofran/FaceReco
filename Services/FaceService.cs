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
                List<FaceUser> list = new List<FaceUser>();

                using (conn)
                {
                    MySqlCommand cmd = new MySqlCommand("select * from FaceUser", conn);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new FaceUser()
                            {
                                Id = reader["Id"].ToString(),
                                FirstName = reader["FirstName"].ToString(),
                                LastName = reader["LastName"].ToString()
                            });
                        }
                    }
                }
                return list;
            }
            return new List<FaceUser>();
        }

        public void AddFaceUsers(FaceUser FaceUser)
        {
            if (_context.FaceUsers != null)
            {
                using (conn)
                {
                    MySqlCommand cmd = new MySqlCommand
                    ("insert into FaceUser (Id,Lastname,Firstname,Face) VALUES (@id, @Lastname, @Firstname, @Face)", conn);
                    cmd.Parameters.AddWithValue("@id", FaceUser.Id);
                    cmd.Parameters.AddWithValue("@Lastname", FaceUser.LastName);
                    cmd.Parameters.AddWithValue("@Firstname", FaceUser.FirstName);
                    cmd.Parameters.AddWithValue("@Face", FaceUser.Face);
                    cmd.ExecuteNonQuery();
                }
                //_context.FaceUsers.Add(FaceUser);
                //_context.SaveChanges();
            }
        }

        public void AddFoto(byte[] foto)
        {
            if (foto != null)
            {
                using (conn)
                {
                    MySqlCommand cmd = new MySqlCommand
                    ("insert into FaceUser (Id,Lastname,Firstname,Face) VALUES (@id, Prova, Prova, @Face)", conn);
                    cmd.Parameters.AddWithValue("@id", 5);
                    cmd.Parameters.AddWithValue("@Face", foto);
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
                        MySqlCommand cmd = new MySqlCommand(query, conn);
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
