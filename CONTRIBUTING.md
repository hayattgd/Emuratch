# Contributing guide

Please write programs with following rules.

## 1. Commit prefix

Commit message should be:
```
(prefix): (description)
```

| Prefix  | Description                                                                         |
|:-------:|:-----------------------------------------------------------------------------------:|
| docs    | Changes to documents (like README.md)                                               |
| fix     | Fix / patches for known issues                                                      |
| style   | Code style changes (indents, brackets)                                              |
| perf    | Performance Improvements                                                            |
| refactor| Refactoring that doesn't affect on behavior                                         |
| improve | Improvements that changes behavior (please prefer "perf" or "refactor" if possible) |
| feat    | New feature and not improvements                                                    |
| chore   | Others                                                                              |

## 2. Namespace
- `Emuratch.Core` : Scratch Logics (or only helper for rendering)
- `Emuratch.Render` : Only rendering comes here, no logics are allowed.
- `Emuratch.UI` : Contains User interface like Dialogs.

## 3. Adding Engine / Render

### Adding engine

To add new engine (like JIT compiler, cached Interpreter) write new code implements `IRunner` on `src/Core/vm/`. If there is a reason you can't / don't want to implement `IRunner`, please describe a reason when making PR.

### Adding render

To add new render, write a code implements `IRender` on `src/Render/`. Should be named like `RaylibRender`, not `Raylib`. Same as adding a engine, please describe a reason if you can't / don't want to implement `IRender` or naming like `XXXRender`.

## 4. Testing

Please run [Scratch](https://scratch.mit.edu/projects/editor) without any extension enabled on chrome / chromium.
