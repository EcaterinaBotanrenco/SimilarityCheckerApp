using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimilarityChecker.Shared.Dtos
{
    public sealed class UserProfileDto
    {
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string[] Roles { get; set; } = Array.Empty<string>();

        public List<UserReportItemDto> Reports { get; set; } = new();
    }
}
