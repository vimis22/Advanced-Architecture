import React from "react";
import Navbar from "../../screenparts/Navbar";
import OrderForm from "../../functions/OrderForm";

const OrderScreen: React.FC = () => {
  return (
    <div>
      <Navbar />
      <div style={{ padding: "16px" }}>
        <h1>Create Book Order</h1>
        <OrderForm />
      </div>
    </div>
  );
};

export default OrderScreen;
