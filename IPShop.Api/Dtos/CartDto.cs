namespace IPShop.Api.Dtos;

public class CartDto
{
}
public class AddToCartRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
