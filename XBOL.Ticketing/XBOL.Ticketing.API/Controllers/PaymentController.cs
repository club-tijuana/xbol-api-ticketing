using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.DTO.EvoPayment;
using XBOL.Ticketing.Services.EvoPayment;

namespace XBOL.Ticketing.API.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentController(IEvoPaymentService evoPaymentService) : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("create-checkout-session")]
        [ProducesResponseType(typeof(CheckoutSessionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<CheckoutSessionResponse>> CreateCheckoutSessionAsync(
            [FromBody] CreateCheckoutSessionRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await evoPaymentService.CreateCheckoutSessionAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("initiate-checkout")]
        [ProducesResponseType(typeof(InitiateCheckoutResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<InitiateCheckoutResponse>> InitiateCheckoutAsync(
            [FromBody] InitiateCheckoutRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await evoPaymentService.InitiateCheckoutAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("confirm-checkout")]
        [ProducesResponseType(typeof(ConfirmCheckoutResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<ConfirmCheckoutResponse>> ConfirmCheckoutAsync(
            [FromBody] ConfirmCheckoutRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await evoPaymentService.ConfirmCheckoutAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("retrieve-order/{orderRefId}")]
        [ProducesResponseType(typeof(RetrieveOrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<RetrieveOrderResponse>> RetrieveOrderAsync(
            [FromRoute] string orderRefId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(orderRefId))
            {
                return BadRequest(new { error = "orderRefId es requerido." });
            }

            try
            {
                var result = await evoPaymentService.RetrieveOrderAsync(orderRefId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
            }
        }
    }
}
