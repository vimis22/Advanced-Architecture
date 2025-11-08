import React, { useState } from "react";

function ProductionOrderForm() {
  const [form, setForm] = useState({
    customerRef: "",
    bookTitle: "",
    quantity: 1,
    priority: "NORMAL"
  });

  function handleChange(event) {
    const { name, value } = event.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  }

  function handleSubmit(event) {
    event.preventDefault();
    console.log("Production order:", form);
    alert("Production order created (currently only logged in console).");
  }

  return (
    <form className="order-form" onSubmit={handleSubmit}>
      <div className="form-row">
        <label>Customer Ref</label>
        <input
          name="customerRef"
          value={form.customerRef}
          onChange={handleChange}
          placeholder="ACME-PO-42"
          required
        />
      </div>

      <div className="form-row">
        <label>Book Title</label>
        <input
          name="bookTitle"
          value={form.bookTitle}
          onChange={handleChange}
          placeholder="Intro to Kafka"
          required
        />
      </div>

      <div className="form-row">
        <label>Quantity</label>
        <input
          type="number"
          name="quantity"
          min="1"
          value={form.quantity}
          onChange={handleChange}
          required
        />
      </div>

      <div className="form-row">
        <label>Priority</label>
        <select
          name="priority"
          value={form.priority}
          onChange={handleChange}
        >
          <option value="LOW">LOW</option>
          <option value="NORMAL">NORMAL</option>
          <option value="HIGH">HIGH</option>
        </select>
      </div>

      <button type="submit" className="submit-btn">
        Create Production Order
      </button>
    </form>
  );
}

export default ProductionOrderForm;
