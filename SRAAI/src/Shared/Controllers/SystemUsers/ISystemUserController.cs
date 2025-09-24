using SRAAI.Shared.Dtos.Identity;  
  
namespace SRAAI.Shared.Controllers.SystemUsers;  
  
[Route("api/[controller]/[action]/"), AuthorizedApi]  
public interface ISystemUserController : IAppController  
{  
   [HttpGet("{id}")]  
   Task<UserDto> Get(Guid id, CancellationToken cancellationToken);  
  
   [HttpPost]  
   Task<UserDto> Create(UserDto dto, CancellationToken cancellationToken);  
  
   [HttpPut]  
   Task<UserDto> Update(UserDto dto, CancellationToken cancellationToken);  
  
   [HttpDelete("{id}")]  
   Task Delete(Guid id, CancellationToken cancellationToken);
  
   [HttpGet]  
   Task<PagedResult<UserDto>> GetUsers(CancellationToken cancellationToken) => default!;  
  
   [HttpGet]  
   Task<List<UserDto>> Get(CancellationToken cancellationToken) => default!;  
}
