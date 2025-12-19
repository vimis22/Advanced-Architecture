// CURRENT REQUEST PAYLOAD (flat) sent to API-Gateway → Orchestrator
// This mirrors the current backend DTO while aligning enum values with the final plan.
// Final plan: move to nested `books` JSON and snake_case across the entire pipeline.
// Until the backend endpoint switches, we keep this flat structure to stay backward-compatible.
import type { CoverType, PageType } from "./OrderFormState";

interface Payload {
    title: string;
    author: string;
    pages: number;
    coverType: CoverType; // HARDCOVER | SOFTCOVER
    pageType: PageType;   // GLOSSY | MATTE
    quantity: number;
}

export default Payload;
