using SRAAI.Shared.Controllers.SystemUsers;  
using SRAAI.Shared.Dtos.Identity;  
  
namespace SRAAI.Server.Api.Controllers.SystemUsers;  
  
[ApiController, Route("api/[controller]/[action]"), Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]  
public partial class SystemUserController : AppControllerBase, ISystemUserController
{  
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
      var entityToAdd = dto.Map();  
  
      await DbContext.Users.AddAsync(entityToAdd, cancellationToken);  
  
      await DbContext.SaveChangesAsync(cancellationToken);  
  
      return entityToAdd.Map();  
   }  
  
   [HttpPut]  
   public async Task<UserDto> Update(UserDto dto, CancellationToken cancellationToken)  
   {  
       var entityToUpdate = dto.Map();

       DbContext.Update(entityToUpdate);

       await DbContext.SaveChangesAsync(cancellationToken);

       
       return entityToUpdate.Map();
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
