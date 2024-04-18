using System;
using System.Net;
using System.Threading.Tasks;
using idunno.Authentication.Basic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mluvii.GenericChannelDemo.Web.Models;
using mluvii.GenericChannelDemo.Web.Services;
using mluvii.GenericChannelModels.Activity;
using mluvii.GenericChannelModels.Registration;

namespace mluvii.GenericChannelDemo.Web.Areas.api.Controllers
{
    [ApiExplorerSettings(GroupName = "api")]
    [Area("api")]
    [Route("/api/[controller]/[action]")]
    [Authorize(AuthenticationSchemes = BasicAuthenticationDefaults.AuthenticationScheme, Roles = "GenericChannel")]
    public class GenericChannelController : Controller
    {
        private readonly IChatService chatService;

        public GenericChannelController(IChatService chatService)
        {
            this.chatService = chatService;
        }

        [HttpPost]
        public async Task<IActionResult> Webhook([FromBody] GenericChannelWebhookRegistrationModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await chatService.RegisterWebhook(model.Url, model.Headers);
            return Ok();
        }

        [HttpPost]
        [ProducesResponseType(typeof(GenericChannelActivityResponse), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> Activity([FromBody] GenericChannelActivity model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var messageId = await Receive(model);

            return new ObjectResult(new GenericChannelActivityResponse
            {
                Id = messageId
            });
        }

        private async Task<string> Receive(GenericChannelActivity model)
        {
            switch (model.Type)
            {
                case GenericChannelActivityType.ChatMessage:
                    return await chatService.ReceiveMessage(model.ConversationId, new MessageModel
                    {
                        MessageType = MessageType.Received,
                        Timestamp = DateTimeOffset.Now,
                        Content = model.Text
                    });
                case GenericChannelActivityType.OperatorJoined:
                    return await chatService.ReceiveMessage(model.ConversationId, new MessageModel
                    {
                        MessageType = MessageType.System,
                        Timestamp = DateTimeOffset.Now,
                        Content = $@"<img src=""{model.OperatorPfp}""></img>{model.OperatorUserFullName} has joined"
                    });
                case GenericChannelActivityType.OperatorLeft:
                    return await chatService.ReceiveMessage(model.ConversationId, new MessageModel
                    {
                        MessageType = MessageType.System,
                        Timestamp = DateTimeOffset.Now,
                        Content = $@"<img src=""{model.OperatorPfp}""></img>{model.OperatorUserFullName} has left"
                    });
                default:
                    return null;
            }
        }
    }
}
