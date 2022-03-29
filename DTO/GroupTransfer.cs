using SWZZ_Backend.Models;

namespace SWZZ_Backend.DTO
{
    public class GroupDTO
    {
        public int GroupId { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }

        public string Description {get; set;}
        public GroupDTO() {}
        
        public GroupDTO(Group group)
        {
            GroupId = group.Id;
            Name = group.Name;
            Icon = group.Icon;
            Description = group.Description;
        }

        public Group toGroup(string code)
        {
            return new Group() { Id = GroupId, Name = Name, Icon = Icon, Description = Description, Code = code };
        }
    }

    public class GroupAndPermissionsDTO
    {
        public GroupDTO GroupDTO { get; set; }
        public GroupPermissions GroupPermissions { get; set; }

        public GroupAndPermissionsDTO() {}
        public GroupAndPermissionsDTO(GroupDTO groupDTO, GroupPermissions permissions) => (GroupDTO, GroupPermissions) = (groupDTO, permissions); 
    }
}