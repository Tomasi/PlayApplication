using Microsoft.AspNetCore.Mvc;
using Play.Common;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Controllers
{
    [ApiController]
    [Route("itens")]
    public class ItensController : ControllerBase
    {
        private readonly IRepository<InventoryItem> inventoryItensRepository;
        private readonly IRepository<CatalogItem> catalogItensRepository;

        public ItensController(IRepository<InventoryItem> inventoryItensRepository, IRepository<CatalogItem> catalogItensRepository)
        {
            this.inventoryItensRepository = inventoryItensRepository;
            this.catalogItensRepository = catalogItensRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest();
            }

            var inventoryItemEntities = await inventoryItensRepository.GetAllAsync(item => item.UserId == userId);
            var itemIds = inventoryItemEntities.Select(item => item.CatalogItemId);
            var catalogItemEntities = await catalogItensRepository.GetAllAsync(item => itemIds.Contains(item.Id));

            var inventoryItemDtos = inventoryItemEntities.Select(inventoryItem =>
            {
                var catalogItem = catalogItemEntities.Single(catalogItem => catalogItem.Id == inventoryItem.CatalogItemId);
                return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
            });

            return Ok(inventoryItemDtos);
        }

        [HttpPost]
        public async Task<ActionResult> PostAsync(GrantItensDto grantItensDto)
        {
            var inventoryItem = await inventoryItensRepository.GetAsync(item => item.UserId == grantItensDto.UserId &&
                                                               item.CatalogItemId == grantItensDto.CatalogItemId);

            if (inventoryItem == null)
            {
                inventoryItem = new InventoryItem
                {
                    CatalogItemId = grantItensDto.CatalogItemId,
                    UserId = grantItensDto.UserId,
                    Quantity = grantItensDto.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow
                };

                await inventoryItensRepository.CreateAsync(inventoryItem);
            }
            else
            {
                inventoryItem.Quantity += grantItensDto.Quantity;
                await inventoryItensRepository.UpdateAsync(inventoryItem);
            }
            return Ok();
        }
    }
}