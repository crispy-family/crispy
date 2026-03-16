using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Identity;

namespace Crispy.Core.Entities
{
    public class User : IdentityUser<int>
    {
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public string? ProfilePhoto { get; set; }
        public string? Bio { get; set; }
        public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
