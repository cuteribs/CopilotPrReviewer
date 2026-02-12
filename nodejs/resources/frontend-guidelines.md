# Frontend Code Review Guidelines

Use these guidelines when reviewing JavaScript, TypeScript, React, HTML, and CSS code.

## Security

### Critical Issues
- **XSS (Cross-Site Scripting)**: Never use `dangerouslySetInnerHTML` without sanitization
  ```tsx
  // BAD
  <div dangerouslySetInnerHTML={{ __html: userInput }} />
  
  // GOOD - use DOMPurify or similar
  import DOMPurify from 'dompurify';
  <div dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(userInput) }} />
  ```

- **Eval and Dynamic Code**: Avoid `eval()`, `Function()`, `setTimeout(string)`
  ```javascript
  // BAD
  eval(userInput);
  
  // GOOD
  // Use proper parsing or predefined functions
  ```

- **Hardcoded Secrets**: No API keys, tokens, or secrets in frontend code
  ```javascript
  // BAD
  const API_KEY = 'sk-1234567890';
  
  // GOOD
  // Use environment variables or backend proxy
  const API_KEY = process.env.REACT_APP_API_KEY;
  ```

- **Insecure URLs**: Check for `http://` usage where `https://` is required
- **innerHTML Assignment**: Use `textContent` instead of `innerHTML` for text

### Major Issues
- **Missing Input Validation**: Validate user inputs on frontend (and backend)
- **Exposed Sensitive Data**: Don't log or display sensitive information
- **CORS Issues**: Verify proper CORS configuration for API calls

## Code Quality

### TypeScript
- Avoid `any` type - use proper typing or `unknown`
- Use strict mode (`"strict": true` in tsconfig)
- Define interfaces for complex objects
- Use type guards for runtime type checking

### React Specific
- **Keys in Lists**: Always use stable, unique keys (not array index)
  ```tsx
  // BAD
  items.map((item, index) => <Item key={index} />)
  
  // GOOD
  items.map(item => <Item key={item.id} />)
  ```

- **Dependency Arrays**: Include all dependencies in useEffect/useMemo/useCallback
  ```tsx
  // BAD - missing dependency
  useEffect(() => {
    fetchData(userId);
  }, []); // userId missing
  
  // GOOD
  useEffect(() => {
    fetchData(userId);
  }, [userId]);
  ```

- **Conditional Hooks**: Never call hooks conditionally
- **State Updates**: Use functional updates when new state depends on old
  ```tsx
  // BAD
  setCount(count + 1);
  
  // GOOD
  setCount(prev => prev + 1);
  ```

### JavaScript
- Use `===` instead of `==` for comparisons
- Use `const` by default, `let` when reassignment needed, avoid `var`
- Use optional chaining (`?.`) and nullish coalescing (`??`)
- Prefer `async/await` over `.then()` chains

## Performance

### React Performance
- **Unnecessary Rerenders**: Use `React.memo`, `useMemo`, `useCallback` appropriately
- **Large Lists**: Use virtualization (react-window, react-virtualized)
- **Bundle Size**: Check for large dependencies, use code splitting
- **Images**: Verify lazy loading and appropriate sizes

### JavaScript Performance
- Avoid creating functions in render/loops
- Debounce/throttle expensive event handlers
- Use Web Workers for heavy computations
- Avoid layout thrashing (batch DOM reads/writes)

### CSS Performance
- Avoid expensive selectors (deep nesting, universal selectors)
- Use `transform` and `opacity` for animations (GPU accelerated)
- Avoid `!important` except for overrides

## Logic Errors

### Common Issues
- **Truthiness Bugs**: `0`, `""`, `null`, `undefined` are falsy
  ```javascript
  // BAD - fails for count = 0
  if (count) { ... }
  
  // GOOD
  if (count !== undefined) { ... }
  ```

- **Async Race Conditions**: Verify cleanup in useEffect, handle stale closures
- **Event Handler Binding**: Ensure proper `this` binding or use arrow functions
- **Promise Error Handling**: Always handle promise rejections

### State Management
- Don't mutate state directly
- Handle loading and error states
- Verify proper state initialization

## HTML/Accessibility

### Critical
- **Missing Alt Text**: All images need descriptive alt attributes
- **Form Labels**: All form inputs need associated labels
- **Semantic HTML**: Use proper elements (button, nav, main, etc.)

### Major
- **Keyboard Navigation**: Interactive elements must be keyboard accessible
- **Focus Management**: Proper focus handling for modals/dialogs
- **Color Contrast**: Verify sufficient color contrast ratios

## Best Practices

### Code Organization
- Keep components small and focused
- Extract reusable logic into custom hooks
- Separate concerns (logic, presentation, data fetching)

### Error Handling
- Use Error Boundaries for React components
- Display user-friendly error messages
- Log errors appropriately

### Testing
- Components should be testable
- Avoid tight coupling to external services
- Use proper data-testid attributes

---

*[TODO: Customize these guidelines based on your team's specific standards, frameworks, and component libraries]*
