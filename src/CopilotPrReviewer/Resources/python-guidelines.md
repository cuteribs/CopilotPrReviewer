# Python Code Review Guidelines

Use these guidelines when reviewing Python code.

## Security

### Critical Issues
- **SQL Injection**: Never use string formatting for SQL queries
  ```python
  # BAD
  cursor.execute(f"SELECT * FROM users WHERE id = {user_id}")
  
  # GOOD
  cursor.execute("SELECT * FROM users WHERE id = %s", (user_id,))
  ```

- **Command Injection**: Avoid `os.system()`, `subprocess.shell=True`
  ```python
  # BAD
  os.system(f"rm {user_input}")
  subprocess.run(f"ls {path}", shell=True)
  
  # GOOD
  subprocess.run(["rm", user_input], shell=False)
  ```

- **Hardcoded Secrets**: No passwords, API keys, or tokens in code
  ```python
  # BAD
  API_KEY = "sk-1234567890"
  
  # GOOD
  API_KEY = os.environ.get("API_KEY")
  ```

- **Pickle/Eval**: Never unpickle or eval untrusted data
  ```python
  # BAD
  data = pickle.loads(untrusted_data)
  eval(user_input)
  
  # GOOD
  data = json.loads(untrusted_data)
  ```

- **Path Traversal**: Validate file paths
  ```python
  # BAD
  with open(os.path.join(base_dir, user_input)) as f:
  
  # GOOD
  safe_path = os.path.realpath(os.path.join(base_dir, user_input))
  if not safe_path.startswith(os.path.realpath(base_dir)):
      raise ValueError("Invalid path")
  ```

### High Issues
- **Insecure Random**: Use `secrets` module for security-sensitive randomness
- **XML Parsing**: Disable external entities (use `defusedxml`)
- **YAML Loading**: Use `yaml.safe_load()` instead of `yaml.load()`

## Code Quality

### Type Hints
- Use type hints for function signatures
- Use `Optional[T]` for nullable types (or `T | None` in 3.10+)
- Define TypedDict or dataclasses for complex structures

### Naming Conventions
- `snake_case` for functions, variables, modules
- `PascalCase` for classes
- `UPPER_CASE` for constants
- Prefix private members with `_`

### Functions
- Keep functions focused and small
- Use keyword arguments for clarity
- Document with docstrings (Google or NumPy style)
- Avoid mutable default arguments
  ```python
  # BAD
  def func(items=[]):
      items.append(1)
  
  # GOOD
  def func(items=None):
      items = items or []
  ```

### Exception Handling
- Catch specific exceptions, not bare `except:`
- Use context managers (`with`) for resources
- Don't silently swallow exceptions
  ```python
  # BAD
  try:
      do_something()
  except:
      pass
  
  # GOOD
  try:
      do_something()
  except ValueError as e:
      logger.error(f"Value error: {e}")
      raise
  ```

## Performance

### Data Structures
- Use appropriate collections (list vs set vs dict)
- Use generators for large sequences
- Use `collections.deque` for queue operations
- Consider `dataclasses` over plain dicts for structured data

### String Operations
- Use f-strings for formatting (Python 3.6+)
- Use `"".join()` for concatenating many strings
- Use `str.startswith()`/`str.endswith()` tuples for multiple checks

### Iteration
- Use list/dict/set comprehensions when appropriate
- Avoid repeated lookups in loops
- Use `enumerate()` instead of manual indexing
- Use `zip()` for parallel iteration

### Memory
- Use generators for large data processing
- Close files and connections properly
- Consider `__slots__` for memory-heavy classes

## Logic Errors

### Common Issues
- **Off-by-one Errors**: Check range boundaries
- **Mutable Default Arguments**: Lists/dicts as defaults are shared
- **Integer Division**: Use `//` for integer division, `/` returns float
- **Boolean Gotchas**: Empty list/dict/string are falsy

### Concurrency
- Check for race conditions in threaded code
- Use proper locking mechanisms
- Verify async/await usage is correct
- Handle task cancellation properly

## Best Practices

### Code Organization
- Follow single responsibility principle
- Use meaningful variable names
- Keep modules focused
- Avoid circular imports

### Dependency Injection
- Don't hardcode dependencies
- Use dependency injection for testability
- Avoid global state

### Logging
- Use `logging` module, not `print()`
- Log at appropriate levels (DEBUG, INFO, WARNING, ERROR)
- Include context in log messages

### Testing
- Write testable code (avoid tight coupling)
- Use fixtures for test data
- Mock external dependencies

---

*[TODO: Customize these guidelines based on your team's specific standards, frameworks (Django, FastAPI, etc.), and Python version]*
