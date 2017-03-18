﻿module FunnyScript.Eval
open AST

let rec eval expr env =
  let apply f arg =
    match f with
    | Func (BuiltinFunc f) -> f.Apply arg
    | Func (UserFunc f) -> f.Env |> Map.add (Name f.Def.Arg) arg |> eval f.Def.Body
    | _ -> None
  match expr with
  | Obj x -> Some x
  | Ref x -> env |> Map.tryFind (Name x) |> function Some x -> Some x | _ -> printfn "'%s' is not found." x; None
  | RefMember (expr, name) ->
    env |> eval expr
    |> Option.bind (function
      | Record r -> r |> Map.tryFind name
      | _ -> None)
  | Let (name, value, succ) ->
    env |> eval value
    |> Option.bind (fun value ->
      let env = env |> Map.add (Name name) value
      match value with Func (UserFunc f) -> f.Env <- env | _ -> ()  // to enable recursive call
      env |> eval succ)
  | Combine (expr1, expr2) ->
    env |> eval expr1 |> ignore
    env |> eval expr2
  | FuncDef def -> Func (UserFunc { Def = def; Env = env }) |> Some
  | Apply (f, arg) ->
    env |> eval f
    |> Option.bind (fun f -> env |> eval arg |> Option.bind (apply f))
  | BinaryOp (op, expr1, expr2) ->
    env |> Map.tryFind (Op op)
    |> Option.bind (fun f -> env |> eval expr1 |> Option.bind (apply f))
    |> Option.bind (fun f -> env |> eval expr2 |> Option.bind (apply f))
  | If (cond, thenExpr, elseExpr) ->
    match env |> eval cond with
    | Some True  -> env |> eval thenExpr
    | Some False -> env |> eval elseExpr
    | _ -> None
  | NewTuple fields ->
    let fields = fields |> Array.map (fun expr -> env |> eval expr)
    if fields |> Array.forall Option.isSome
      then fields |> Array.map Option.get |> Tuple |> Some
      else None
  | NewRecord fields ->
    let fields = fields |> Array.map (fun (name, expr) -> env |> eval expr |> Option.map (fun x -> name, x))
    if fields |> Array.forall Option.isSome
      then fields |> Array.map Option.get |> Map.ofArray |> Record |> Some
      else None
  | _ -> None