#The purpose is to conduct a statistical test.
#The purpose is to understand the orders completion time depending on the size, waiting time, amount of errors (requeues).
#Multiple Lineare Regression catches real complexity as simple tests such as Lineare Regression Tests cannot do.

#The test helps us in answering research questions.
# 1. Does the amount of orders influence the performance?
# 2. Does the amount of requeues have a significant meaning for performance?
# 3. Does waiting time have an influence on performance?
# 4. Which factor is the most important?

orders <- read.csv("C:/Users/vivek/Downloads/orders.csv")
str(orders)
head(orders)

summary(orders$quantity)
summary(orders$completion_time_minutes)
summary(orders$total_requeues)
summary(orders$wait_time_seconds)

sd(orders$quantity)
sd(orders$completion_time_minutes)
sd(orders$total_requeues)
sd(orders$wait_time_seconds)

regression_orders <- lm(completion_time_minutes ~ quantity + total_requeues + wait_time_seconds, data=orders)
summary(regression_orders)

hist(regression_orders$residuals,
     main = "Histogram of Residuals",
     xlab = "Residuals")

qqnorm(regression_orders$residuals)
qqline(regression_orders$residuals, col = "red")

plot(regression_orders$fitted.values, regression_orders$residuals,
     xlab = "Fitted Values",
     ylab = "Residuals",
     main = "Residuals vs. Fitted")
abline(h = 0, col = "red")

cor(orders[, c("quantity","total_requeues","wait_time_seconds","completion_time_minutes")])
confint(regression_orders, level = 0.9)

