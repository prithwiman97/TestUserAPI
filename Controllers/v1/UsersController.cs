using Microsoft.AspNetCore.Mvc;
using TestUserAPI.Models.Requests;
using TestUserAPI.Models.Responses;
using TestUserAPI.Repositories;

namespace TestUserAPI.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserRepository userRepository, ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all users with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var result = await _userRepository.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Search users by username (partial match, case-insensitive)
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<UserResponse>>> SearchUsers(
        [FromBody] SearchUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new { message = "Username is required" });
        }

        if (request.Page < 1) request.Page = 1;
        if (request.PageSize < 1 || request.PageSize > 100) request.PageSize = 10;

        var result = await _userRepository.SearchByUsernameAsync(request.Username, request.Page, request.PageSize);
        return Ok(result);
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new { message = "Username is required" });
        }

        try
        {
            var user = await _userRepository.CreateAsync(request);
            return CreatedAtAction(nameof(GetAllUsers), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating user");
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "An error occurred while creating the user" });
        }
    }

    /// <summary>
    /// Update user details by username
    /// </summary>
    /// <param name="username">Username of the user to update</param>
    [HttpPut("{username}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponse>> UpdateUser(
        string username,
        [FromBody] UpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new { message = "Username is required" });
        }

        var user = await _userRepository.UpdateByUsernameAsync(username, request);
        
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(user);
    }
}

