import React from "react";
import NormalText from "../screenparts/NormalText";
import { useOrderForm } from "./FunctionMethods";

const OrderForm: React.FC = () => {
  const { form, loading, error, result, handleChange, handleSubmit } = useOrderForm();

  return (
      <div style={{display: 'flex', flexDirection: 'column', alignItems: 'center'}}>
      <NormalText text="Order Form" textColor="#ffffff" fontSize={18} fontWeight="bold"
        textAlign="center" containerHeight="50px" containerWidth="100%" containerBackgroundColor="#330099"/>

      <form
        onSubmit={handleSubmit}
        style={{marginTop: "16px", display: "flex", flexDirection: "column", gap: "8px", maxWidth: "320px"}}>
          <NormalText text={'Title'} textColor={'#000000'} fontSize={12} fontWeight={10} textAlign={'center'} />
        <input name="title" value={form.title} onChange={handleChange} required placeholder="Title" />
          <NormalText text={'Author'} textColor={'#000000'} fontSize={12} fontWeight={10} textAlign={'center'} />
        <input name="author" value={form.author} onChange={handleChange} required placeholder="Author" />
          <NormalText text={'Pages'} textColor={'#000000'} fontSize={12} fontWeight={10} textAlign={'center'} />
        <input name="pages" type="number" value={form.pages} onChange={handleChange} required placeholder="Pages" />
          <NormalText text={'Cover Type'} textColor={'#000000'} fontSize={12} fontWeight={10} textAlign={'center'} />
        <select name="coverType" value={form.coverType} onChange={handleChange}>
          <option value="SOFTCOVER">SOFTCOVER</option>
          <option value="HARDCOVER">HARDCOVER</option>
        </select>

        <NormalText text={'Page Type'} textColor={'#000000'} fontSize={12} fontWeight={10} textAlign={'center'} />
        <select name="pageType" value={form.pageType} onChange={handleChange}>
          <option value="GLOSSY">GLOSSY</option>
          <option value="MATTE">MATTE</option>
        </select>
        <NormalText text={'Quantity'} textColor={'#000000'} fontSize={12} fontWeight={10} textAlign={'center'} />
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
