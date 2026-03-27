document.addEventListener('DOMContentLoaded', () => {
  console.log('Frontend loaded');
});

import { getProducts, createCart, addToCartApi } from './src/api.js';

const CART_KEY = "cartId";

// ---------- CART HELPERS ----------
async function getOrCreateCart() {
    let cartId = localStorage.getItem(CART_KEY);

    if (!cartId) {
        const cart = await createCart();
        cartId = cart.id;
        localStorage.setItem(CART_KEY, cartId);
    }

    return cartId;
}

// ---------- LOAD PRODUCTS ----------
async function loadProducts() {
    const container = document.getElementById('products');

    try {
        const products = await getProducts();

        container.innerHTML = products.map(p => `
            <div class="product-card">
                <h3>${p.name}</h3>
                <p>${p.description ?? ""}</p>
                <p>€${p.price}</p>
                <button onclick="addToCart(${p.id})">Add to cart</button>
            </div>
        `).join("");

    } catch (error) {
        console.error(error);
        container.innerHTML = "<p>Error loading products</p>";
    }
}

// ---------- ADD TO CART ----------
window.addToCart = async function (productId) {
    try {
        const cartId = await getOrCreateCart();

        await addToCartApi(cartId, productId, 1);

        alert("Added to cart ✅");

    } catch (error) {
        console.error(error);
        alert("Failed to add to cart ❌");
    }
};

// ---------- INIT ----------
document.addEventListener("DOMContentLoaded", loadProducts);