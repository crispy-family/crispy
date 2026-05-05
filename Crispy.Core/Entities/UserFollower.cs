using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crispy.Core.Entities
{
    public class UserFollower
    {
        public int FollowerId { get; set; }
        public User? Follower { get; set; }

        public int FollowedUserId { get; set; }
        public User? FollowedUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
    }
}
