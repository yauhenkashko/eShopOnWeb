using System.Configuration;
using System.Text.Json;
using Ardalis.GuardClauses;
using AutoMapper;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.eShopWeb.ApplicationCore.DTO;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.Infrastructure.Identity;
using Microsoft.eShopWeb.Web.Interfaces;
using Microsoft.Extensions.Options;

namespace Microsoft.eShopWeb.Web.Pages.Basket;

[Authorize]
public class CheckoutModel : PageModel
{
    private readonly IBasketService _basketService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOrderService _orderService;
    private string? _username = null;
    private readonly IBasketViewModelService _basketViewModelService;
    private readonly IAppLogger<CheckoutModel> _logger;
    private readonly HttpClient _httpClient;
    private readonly IMapper _mapper;
    private readonly DeliveryServiceConfiguration _deliveryConfiguration;
    private readonly ReserverServiceConfiguration _reserverConfiguration;

    public CheckoutModel(IBasketService basketService,
        IBasketViewModelService basketViewModelService,
        SignInManager<ApplicationUser> signInManager,
        IOrderService orderService,
        IAppLogger<CheckoutModel> logger,
        HttpClient httpClient,
        IMapper mapper,
        IOptions<DeliveryServiceConfiguration> deliveryConfiguration,
        IOptions<ReserverServiceConfiguration> reserverConfiguration)
    {
        _basketService = basketService;
        _signInManager = signInManager;
        _orderService = orderService;
        _basketViewModelService = basketViewModelService;
        _logger = logger;
        _httpClient = httpClient;
        _mapper = mapper;
        _deliveryConfiguration = deliveryConfiguration.Value ?? throw new ConfigurationErrorsException();
        _reserverConfiguration = reserverConfiguration.Value ?? throw new ConfigurationErrorsException();
    }

    public BasketViewModel BasketModel { get; set; } = new BasketViewModel();

    public async Task OnGet()
    {
        await SetBasketModelAsync();
    }

    public async Task<IActionResult> OnPost(IEnumerable<BasketItemViewModel> items)
    {
        try
        {
            await SetBasketModelAsync();

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var updateModel = items.ToDictionary(b => b.Id.ToString(), b => b.Quantity);
            var address = new Address("123 Main St.", "Kent", "OH", "United States", "44240");
            await _basketService.SetQuantities(BasketModel.Id, updateModel);
            await _orderService.CreateOrderAsync(BasketModel.Id, new Address("123 Main St.", "Kent", "OH", "United States", "44240"));
            await _basketService.DeleteBasketAsync(BasketModel.Id);

            var deliveryDetails = _mapper.Map<IEnumerable<BasketItemViewModel>, DeliveryDetailsDto>(items);
            deliveryDetails.DeliveryAddress = address;

            if (_reserverConfiguration.Enabled)
            {
                await SendToReserverService(deliveryDetails);
            }

            if (_deliveryConfiguration.Enabled)
            {
                await SendToDeliveryService(deliveryDetails);
            }
        }
        catch (EmptyBasketOnCheckoutException emptyBasketOnCheckoutException)
        {
            //Redirect to Empty Basket page
            _logger.LogWarning(emptyBasketOnCheckoutException.Message);
            return RedirectToPage("/Basket/Index");
        }

        return RedirectToPage("Success");
    }

    private async Task SendToReserverService(DeliveryDetailsDto deliveryDetails)
    {
        var queueClient = new ServiceBusClient(_reserverConfiguration.ConnectionString);
        var sender = queueClient.CreateSender(_reserverConfiguration.QueueName);
        var json = JsonSerializer.Serialize(deliveryDetails);
        await sender.SendMessageAsync(new ServiceBusMessage(json));
    }

    private async Task SendToDeliveryService(DeliveryDetailsDto deliveryDetails)
    {
        var content = new StringContent(JsonSerializer.Serialize(deliveryDetails), System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(_deliveryConfiguration.DeliveryServiceUrl, content);

        response.EnsureSuccessStatusCode();
    }

    private async Task SetBasketModelAsync()
    {
        Guard.Against.Null(User?.Identity?.Name, nameof(User.Identity.Name));
        if (_signInManager.IsSignedIn(HttpContext.User))
        {
            BasketModel = await _basketViewModelService.GetOrCreateBasketForUser(User.Identity.Name);
        }
        else
        {
            GetOrSetBasketCookieAndUserName();
            BasketModel = await _basketViewModelService.GetOrCreateBasketForUser(_username!);
        }
    }

    private void GetOrSetBasketCookieAndUserName()
    {
        if (Request.Cookies.ContainsKey(Constants.BASKET_COOKIENAME))
        {
            _username = Request.Cookies[Constants.BASKET_COOKIENAME];
        }
        if (_username != null) return;

        _username = Guid.NewGuid().ToString();
        var cookieOptions = new CookieOptions();
        cookieOptions.Expires = DateTime.Today.AddYears(10);
        Response.Cookies.Append(Constants.BASKET_COOKIENAME, _username, cookieOptions);
    }
}
