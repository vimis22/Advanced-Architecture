import React from "react";
import NormalText from "../screenparts/NormalText";
import { useOrderForm } from "./FunctionMethods";

const OrderForm: React.FC = () => {
  const { form, loading, error, result, handleChange, handleSubmit } = useOrderForm();

  return (
    <div>
      <NormalText text="Order Form" textColor="#ffffff" fontSize={18} fontWeight="bold"
        textAlign="center" containerHeight="50px" containerWidth="100%" containerBackgroundColor="#330099"/>

      <form
        onSubmit={handleSubmit}
        style={{marginTop: "16px", display: "flex", flexDirection: "column", gap: "8px", maxWidth: "320px"}}>
        <input name="title" value={form.title} onChange={handleChange} required placeholder="Title" />
        <input name="author" value={form.author} onChange={handleChange} required placeholder="Author" />
        <input name="pages" type="number" value={form.pages} onChange={handleChange} required placeholder="Pages" />
        <select name="coverType" value={form.coverType} onChange={handleChange}>
          <option value="SOFT">SOFT</option>
          <option value="HARD">HARD</option>
        </select>
        <input name="quantity" type="number" value={form.quantity} onChange={handleChange} required placeholder="Quantity" />

        <button type="submit" disabled={loading}>
          {loading ? "Submitting..." : "Submit Order"}
        </button>
      </form>

      {error && <p style={{ color: "red" }}>{error}</p>}
      {result && (
        <div style={{ marginTop: "16px" }}>
          <h3>Order Created</h3>
          <pre>{JSON.stringify(result, null, 2)}</pre>
        </div>
      )}
    </div>
  );
};

export default OrderForm;
