## Agenda

1. How I Use Agentic Engineering in [have-fun](https://github.com/iJustHelp/have-fun/tree/main/design) project.

### Development Workflow

>Goal: make the code human readable and maintainable.

```mermaid
flowchart TD
    BRD([Developer creates brd.md<br/>prompt / requirements]) --> Plan[Agent creates plan.md]
    Plan --> ReviewPlan([Developer reviews plan.md])
    ReviewPlan --> IsBigPlan{Is the plan large?}
    IsBigPlan -- Yes --> SplitStages[Agent splits plan into stages<br>with plan.md]
    SplitStages --> IsStageBig{Is the plan large?}
    IsStageBig -- Yes --> SplitTasks[Agent splits plan into tasks<br>with plan.md]
    SplitTasks -->  ImplementPlan[Agent implements plan.md]
    IsBigPlan -- No --> ImplementPlan
    IsStageBig -- No --> ImplementPlan
    ImplementPlan --> DevReview([Developer reviews and tests code])
    DevReview --> Refactoring([Developer makes refactoring])
    Refactoring --> BRD
```    


    
