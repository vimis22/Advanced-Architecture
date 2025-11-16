// CURRENT UI REQUEST CONTRACT (temporary, flat):
// {
//   title: string,
//   author: string,
//   pages: number,
//   coverType: "HARDCOVER" | "SOFTCOVER",
//   pageType: "GLOSSY" | "MATTE",
//   quantity: number
// }
// Planned Final Unified Contract (target across UI → Gateway → Orchestrator → Kafka):
// {
//   "order_id": string,
//   "timestamp": string,
//   "status": string,
//   "books": {
//     "book_id": string,
//     "title": string,
//     "author": string,
//     "pages": number,
//     "quantity": number,
//     "covertype": "HARDCOVER"|"SOFTCOVER",
//     "pagetype": "GLOSSY"|"MATTE"
//   },
//   "ack_required": boolean
// }
// Note: We keep the flat request for now to avoid a breaking change in the API-Gateway and Orchestrator controllers,
// but we already align enum values (HARDCOVER/SOFTCOVER and GLOSSY/MATTE). The Kafka event will adopt the nested
// structure immediately. Once backend endpoints are updated to accept the nested structure, we will switch the UI
// payload to the nested "books" object and deprecate the flat fields.

export type CoverType = "HARDCOVER" | "SOFTCOVER";
export type PageType = "GLOSSY" | "MATTE";

interface OrderFormState {
    title: string;
    author: string;
    pages: string;
    coverType: CoverType;
    pageType: PageType;
    quantity: string;
}

export default OrderFormState;
