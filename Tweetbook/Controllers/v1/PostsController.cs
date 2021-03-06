﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tweetbook.Contracts.v1;
using Tweetbook.Contracts.v1.Requests;
using Tweetbook.Contracts.v1.Responses;
using Tweetbook.Domain;
using Tweetbook.Extensions;
using Tweetbook.Services.Interface;

namespace Tweetbook.v1.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PostsController : Controller
    {

        private readonly IPostService _postService;

        public PostsController(IPostService postService)
        {
            _postService = postService;
        }
        
        [HttpGet(ApiRoutes.Posts.GetAll)]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _postService.GetPostsAsync());
        }

        [HttpPost(ApiRoutes.Posts.Create)]
        public async Task<IActionResult> Create([FromBody] CreatePostRequest postRequest)
        {
            var newPostId = Guid.NewGuid();
            var post = new Post
            {
                Id = newPostId,
                Name = postRequest.Name,
                UserId = HttpContext.GetUserId(),
                Tags = postRequest.Tags.Select(x => new PostTag{PostId = newPostId, TagName = x}).ToList()
            };
            
            await _postService.CreatePostAsync(post);

            var baseUri = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.ToUriComponent()}";
            var locationUri = baseUri + ApiRoutes.Posts.Get.Replace("{postId}", post.Id.ToString());
            
            var response = new PostResponse
            {
                Id = post.Id
            };
            return Created(locationUri, response);
        }
        
        [HttpGet(ApiRoutes.Posts.Get)]
        public async Task<IActionResult> Get([FromRoute] Guid postId)
        {
            var post = await _postService.GetPostByIdAsync(postId);
            if (post == null)
                return NotFound();
            
            return Ok(post);
        }
        
        [HttpPut(ApiRoutes.Posts.Update)]
        public async Task<IActionResult> Update([FromRoute] Guid postId, [FromBody] UpdatePostRequest request)
        {
            var postOwner = await _postService.UserOwnPostAsync(postId, HttpContext.GetUserId());
            if (!postOwner)
            {
                return BadRequest(new {Error = "You do not own this post."});
            }

            var post = await _postService.GetPostByIdAsync(postId);
            post.Name = request.Name;

            var isUpdated = await _postService.UpdatePostAsync(post);
            if (isUpdated)
                return Ok(post);

            return NotFound();
        }

        [HttpDelete(ApiRoutes.Posts.Delete)]
        public async Task<IActionResult> Delete([FromRoute] Guid postId)
        {
            var postOwner = await _postService.UserOwnPostAsync(postId, HttpContext.GetUserId());
            if (!postOwner)
            {
                return BadRequest(new {Error = "You do not own this post."});
            }
            
            var deleted = await _postService.DeletePostAsync(postId);
            if (deleted)
                return NoContent();

            return NotFound();
        }
    }
}