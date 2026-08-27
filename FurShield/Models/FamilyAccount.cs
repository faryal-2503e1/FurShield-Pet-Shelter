using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 

namespace FurShield.Data
{
    public class FamilyAccount
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FamilyName { get; set; } = string.Empty;

        public string InviteCode { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

        public ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();
    }
}