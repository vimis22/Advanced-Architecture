import { useState } from "react";
import type React from "react";
import OrderFormState from "../interface/OrderFormState";
import OrderResponse from "../interface/OrderResponse";
import Payload from "../interface/Payload";

export function useOrderForm() {
    const [form, setForm] = useState<OrderFormState>({
        title: "",
        author: "",
        pages: "",
        coverType: "",
        pageType: "",
        quantity: "",
    });
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [result, setResult] = useState<OrderResponse | null>(null);

    function handleChange(event: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) {
        const { name, value } = event.target;
        setForm((prev) => ({ ...prev, [name]: value }));
    }

    async function handleSubmit(event: React.FormEvent) {
        event.preventDefault();
        setError(null);
        setResult(null);

        const pagesNumber = Number(form.pages);
        const quantityNumber = Number(form.quantity);

        if (!form.title || !form.author || pagesNumber <= 0 || quantityNumber <= 0) {
            setError("Please fill all fields correctly (pages and quantity must be > 0).");
            return;
        }

        // CURRENT JSON sent to API-Gateway (flat). This is the single source of truth for the UI for now.
        // When backend accepts nested { books: { ... } }, switch here accordingly.
        const body: Payload = {
            title: form.title,
            author: form.author,
            pages: pagesNumber,
            coverType: form.coverType,
            pageType: form.pageType,
            quantity: quantityNumber,
        };

        try {
            setLoading(true);
            // API-Gateway public URL. Gateway forwards body unchanged to Orchestrator at /api/v1/orchestrator/orders
            const res = await fetch("http://localhost:8080/api/v1/orchestrator/orders", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(body),
            });

            if (!res.ok) throw new Error(`Request failed with status ${res.status}`);
            const data = (await res.json()) as OrderResponse;
            setResult(data);
        } catch (err: any) {
            setError(err?.message ?? "Unknown error");
        } finally {
            setLoading(false);
        }
    }

    return { form, setForm, loading, setLoading, error, setError, result, setResult, handleChange, handleSubmit };
}

export default useOrderForm;
