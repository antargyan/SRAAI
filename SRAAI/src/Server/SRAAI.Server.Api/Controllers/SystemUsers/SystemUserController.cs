using SRAAI.Shared.Controllers.SystemUsers;  
using SRAAI.Shared.Dtos.Identity;  
using SRAAI.Server.Api.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace SRAAI.Server.Api.Controllers.SystemUsers;  
  
[ApiController, Route("api/[controller]/[action]"), Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]  
public partial class SystemUserController : AppControllerBase, ISystemUserController
{  
   [AutoInject] private UserManager<User> userManager = default!;
   
   [HttpGet, EnableQuery]  
   public IQueryable<UserDto> Get()  
   {  
      return DbContext.Users 
        .Project();  
   }
 
   [HttpGet]
   public async Task<PagedResult<UserDto>> GetUsers(ODataQueryOptions<UserDto> odataQuery, CancellationToken cancellationToken)
   {
       var query = (IQueryable<UserDto>)odataQuery.ApplyTo(Get(), ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

       var totalCount = await query.LongCountAsync(cancellationToken);

       query = query.SkipIf(odataQuery.Skip is not null, odataQuery.Skip?.Value)
                     .TakeIf(odataQuery.Top is not null, odataQuery.Top?.Value);

       return new PagedResult<UserDto>(await query.ToArrayAsync(cancellationToken), totalCount);
   }

   [HttpGet("{id}")]  
   public async Task<UserDto> Get(Guid id, CancellationToken cancellationToken)  
   {  
      var dto = await Get().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);  
  
      if (dto is null)  
        throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserCouldNotBeFound)]);  
  
      return dto;  
   } 
  
   [HttpPost]  
   public async Task<UserDto> Create(UserDto dto, CancellationToken cancellationToken)  
   {  
      var userToAdd = new User { LockoutEnabled = true };
      
      // Map basic properties
      userToAdd.FullName = dto.FullName;
      userToAdd.Gender = dto.Gender;
      userToAdd.BirthDate = dto.BirthDate;
      userToAdd.HasProfilePicture = dto.HasProfilePicture;
      userToAdd.UserName = dto.UserName;
      userToAdd.Email = dto.Email;
      userToAdd.PhoneNumber = dto.PhoneNumber;
        if (dto.PhoneNumber != null)
        {
            userToAdd.PhoneNumberConfirmed = true;
        }
        if (dto.Email != null)
        {
            userToAdd.EmailConfirmed = true;
        }
      await userManager.CreateUserWithDemoRole(userToAdd, dto.Password);

      return userToAdd.Map();  
   }
  
   [HttpPut]  
   public async Task<UserDto> Update(UserDto dto, CancellationToken cancellationToken)  
   {  
      var existingUser = await DbContext.Users.FindAsync(dto.Id);
      if (existingUser is null)
          throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserCouldNotBeFound)]);

      // Update basic properties
      existingUser.FullName = dto.FullName;
      existingUser.Gender = dto.Gender;
      existingUser.BirthDate = dto.BirthDate;
      existingUser.HasProfilePicture = dto.HasProfilePicture;
      existingUser.Email = dto.Email;
      existingUser.PhoneNumber = dto.PhoneNumber;
      existingUser.UserName = dto.UserName;

      // Update password if provided
      if (!string.IsNullOrEmpty(dto.Password))
      {
          var removePasswordResult = await userManager.RemovePasswordAsync(existingUser);
          if (removePasswordResult.Succeeded)
          {
              var addPasswordResult = await userManager.AddPasswordAsync(existingUser, dto.Password);
              if (!addPasswordResult.Succeeded)
                  throw new ResourceValidationException(addPasswordResult.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
          }
          else
          {
              throw new ResourceValidationException(removePasswordResult.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
          }
      }

      var updateResult = await userManager.UpdateAsync(existingUser);
      if (!updateResult.Succeeded)
          throw new ResourceValidationException(updateResult.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());

      return existingUser.Map();
   }

   [HttpDelete("{id}")]  
   public async Task Delete(Guid id, CancellationToken cancellationToken)  
   {  
      DbContext.Users.Remove(new() { Id = id });  
  
      var affectedRows = await DbContext.SaveChangesAsync(cancellationToken);  
  
      if (affectedRows < 1)  
        throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserCouldNotBeFound)]);  
   }
}
