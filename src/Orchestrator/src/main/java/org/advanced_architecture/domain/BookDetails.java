package org.advanced_architecture.domain;

import jakarta.persistence.Embeddable;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import java.math.BigDecimal;

@Embeddable
public class BookDetails {
    private String title;
    private String author;
    private Integer pages;
    @Enumerated(EnumType.STRING)
    private CoverType coverType;
    @Enumerated(EnumType.STRING)
    private PageType pageType;
    private Integer quantity;
    private BigDecimal estimatedCost;

    protected BookDetails() {
        // For JPA
    }

    public BookDetails(String title, String author, Integer pages, CoverType coverType, PageType pageType, Integer quantity) {
        this.title = title;
        this.author = author;
        this.pages = pages;
        this.coverType = coverType;
        this.pageType = pageType;
        this.quantity = quantity;
        this.estimatedCost = calculateEstimatedCost();
    }

    private BigDecimal calculateEstimatedCost() {
        BigDecimal baseCost = new BigDecimal(pages).multiply(new BigDecimal("0.10"));
        BigDecimal coverCost = (coverType == CoverType.HARDCOVER)
                ? new BigDecimal("5.00")
                : new BigDecimal("2.00");
        BigDecimal pageFinishCost = (pageType == PageType.GLOSSY)
                ? new BigDecimal("1.00")
                : new BigDecimal("0.50");
        return baseCost.add(coverCost).add(pageFinishCost).multiply(new BigDecimal(quantity));
    }

    public String getTitle() { return title; }
    public String getAuthor() { return author; }
    public Integer getPages() { return pages; }
    public CoverType getCoverType() { return coverType; }
    public PageType getPageType() { return pageType; }
    public Integer getQuantity() { return quantity; }
    public BigDecimal getEstimatedCost() { return estimatedCost; }
}
