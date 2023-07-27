using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace FaceRecognition.Data
{
    public class FaceContext : DbContext
    {
        public FaceContext(DbContextOptions<FaceContext> options)
            : base(options)
        {
        }

        public DbSet<FaceRecognition.Models.FaceUser>? FaceUsers { get; set; }
    }
}
