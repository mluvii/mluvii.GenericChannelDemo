using System;
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
    [Area("api")]
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
                        IsIncoming = true,
                        Timestamp = DateTimeOffset.Now,
                        Content = model.Text
                    });
                default:
                    return null;
            }
        }
    }
}
