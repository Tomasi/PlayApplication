using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.Entities;
using Play.Catalog.Contracts;
using Play.Common;

namespace Play.Catalog.Service.Controllers
{
    [ApiController]
    [Route("itens")]
    public class ItensController : ControllerBase
    {
        private readonly IRepository<Item> itensRepository;
        private readonly IPublishEndpoint publishEndpoint;
        public ItensController(IRepository<Item> itensRepository, IPublishEndpoint publishEndpoint)
        {
            this.itensRepository = itensRepository;
            this.publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
        {
            var itens = (await itensRepository.GetAllAsync()).Select(item => item.AsDto());
            return Ok(itens);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            var item = await itensRepository.GetAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return item.AsDto();
        }

        [HttpPost]
        public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto createItemDto)
        {

            var item = new Item
            {
                Id = Guid.NewGuid(),
                Name = createItemDto.Name,
                Price = createItemDto.Price,
                Description = createItemDto.Description,
                CreatedDate = DateTimeOffset.UtcNow
            };
            await itensRepository.CreateAsync(item);
            await publishEndpoint.Publish(new CatalogItemCreated(item.Id, item.Name, item.Description));
            return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
        {
            var existingId = await itensRepository.GetAsync(id);
            if (existingId == null)
            {
                return NotFound();
            }

            existingId.Name = updateItemDto.Name;
            existingId.Price = updateItemDto.Price;
            existingId.Description = updateItemDto.Description;
            await itensRepository.UpdateAsync(existingId);
            await publishEndpoint.Publish(new CatalogItemUpdated(existingId.Id, existingId.Name, existingId.Description));
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var existingId = await itensRepository.GetAsync(id);
            if (existingId == null)
            {
                return NotFound();
            }

            await itensRepository.RemoveAsync(existingId.Id);
            await publishEndpoint.Publish(new CatalogItemDeleted(id));
            return NoContent();
        }
    }
}