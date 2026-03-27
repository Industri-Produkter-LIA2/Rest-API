// Placeholder for API functions.
// We can implement functions here to call the backend API once it's set up.
// (e.g getProducts, getProduct, createProduct, etc.)

// change with your base url
const API_BASE = "https://localhost:7040";

// ---------- PRODUCTS ----------
export async function getProducts() {
    const response = await fetch(`${API_BASE}/products`);
    if (!response.ok) throw new Error("Failed to fetch products");
    return await response.json();
}

// ---------- CART ----------
export async function createCart() {
    const response = await fetch(`${API_BASE}/cart`, {
        method: "POST"
    });

    if (!response.ok) throw new Error("Failed to create cart");

    return await response.json();
}

export async function addToCartApi(cartId, productId, quantity = 1) {
    const response = await fetch(`${API_BASE}/cart/${cartId}/items`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            productId,
            quantity
        })
    });

    if (!response.ok) throw new Error("Failed to add item");

    return await response.json();
}