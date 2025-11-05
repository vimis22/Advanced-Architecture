package org.advanced_architecture.domain;

import jakarta.persistence.Embeddable;
import java.math.BigDecimal;

@Embeddable
public class BookDetails {
    private String title;
    private String author;
    private Integer pages;
    private String coverType;
    private Integer quantity;
    private BigDecimal estimatedCost;

    protected BookDetails() {
        // For JPA
    }

    public BookDetails(String title, String author, Integer pages, String coverType, Integer quantity) {
        this.title = title;
        this.author = author;
        this.pages = pages;
        this.coverType = coverType;
        this.quantity = quantity;
        this.estimatedCost = calculateEstimatedCost();
    }

    private BigDecimal calculateEstimatedCost() {
        BigDecimal baseCost = new BigDecimal(pages).multiply(new BigDecimal("0.10"));
        BigDecimal coverCost = "HARDCOVER".equals(coverType)
                ? new BigDecimal("5.00")
                : new BigDecimal("2.00");
        return baseCost.add(coverCost).multiply(new BigDecimal(quantity));
    }

    public String getTitle() { return title; }
    public String getAuthor() { return author; }
    public Integer getPages() { return pages; }
    public String getCoverType() { return coverType; }
    public Integer getQuantity() { return quantity; }
    public BigDecimal getEstimatedCost() { return estimatedCost; }
}
