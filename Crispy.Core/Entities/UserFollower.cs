using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crispy.Core.Entities
{
    public class UserFollower
    {
        // ID користувача, ЯКИЙ підписується (Читач)
        public int FollowerId { get; set; }
        public User? Follower { get; set; }

        // ID користувача, НА ЯКОГО підписуються (Автор рецептів)
        public int FollowedUserId { get; set; }
        public User? FollowedUser { get; set; }
    }
}
