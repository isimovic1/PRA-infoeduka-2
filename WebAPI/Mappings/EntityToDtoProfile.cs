using AutoMapper;
using WebAPI.DTOs;
using WebAPI.Models;

namespace WebAPI.Mappings
{
    public class EntityToDtoProfile : Profile
    {
        public EntityToDtoProfile()
        {
            // User
            CreateMap<User, UserDto>()
                .ForMember(d => d.RoleName, o => o.MapFrom(s =>
                    s.Role == 2 ? "Admin" : s.Role == 1 ? "Professor" : "Student"));
            CreateMap<UserCreateDto, User>()
                .ForMember(d => d.PasswordHash, o => o.Ignore())
                .ForMember(d => d.IsFirstLogin, o => o.MapFrom(_ => true));
            CreateMap<UserUpdateDto, User>();

            // Group
            CreateMap<Group, GroupDto>();
            CreateMap<GroupCreateDto, Group>()
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Name.Trim()));
            CreateMap<GroupUpdateDto, Group>()
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Name.Trim()));

            // Course
            CreateMap<Course, CourseDto>();
            CreateMap<CourseCreateDto, Course>();
            CreateMap<CourseUpdateDto, Course>();

            // FileAsset → FileAssetDto
            CreateMap<FileAsset, FileAssetDto>()
                .ForMember(d => d.UploadedByEmail, o => o.MapFrom(s => s.UploadedBy != null ? s.UploadedBy.Email : null));

            // Submission → SubmissionDto
            CreateMap<Submission, SubmissionDto>()
                .ForMember(d => d.UploadedAt, o => o.MapFrom(s => s.FileAsset.UploadedAt))
                .ForMember(d => d.FileName, o => o.MapFrom(s => s.FileAsset.FileName))
                .ForMember(d => d.FileUrl, o => o.Ignore());

            // Profile (add)
            CreateMap<Grade, GradeDTO>();

            //Notifikacije
            CreateMap<Notification, NotificationDto>()
            .ForMember(d => d.FromUserEmail,
             o => o.MapFrom(s => s.FromUser != null ? s.FromUser.Email : null));

        }
    }
}
