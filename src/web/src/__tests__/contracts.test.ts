import { describe, it, expect } from 'vitest';
import {
  OfficeHost,
  CommandExecutionTarget,
  CommandStatus,
  AuditSeverity,
  LinkSourceType,
  LinkTargetType,
} from '../types/contracts';

describe('Contract Enums — must match C# values', () => {
  it('OfficeHost values', () => {
    expect(OfficeHost.Unknown).toBe(0);
    expect(OfficeHost.Excel).toBe(1);
    expect(OfficeHost.PowerPoint).toBe(2);
    expect(OfficeHost.Word).toBe(3);
    expect(OfficeHost.Web).toBe(4);
  });

  it('CommandExecutionTarget values', () => {
    expect(CommandExecutionTarget.Sidecar).toBe(0);
    expect(CommandExecutionTarget.WebAddIn).toBe(1);
    expect(CommandExecutionTarget.Backend).toBe(2);
  });

  it('CommandStatus values', () => {
    expect(CommandStatus.Accepted).toBe(0);
    expect(CommandStatus.Completed).toBe(1);
    expect(CommandStatus.Failed).toBe(2);
    expect(CommandStatus.Deferred).toBe(3);
  });

  it('AuditSeverity values', () => {
    expect(AuditSeverity.Debug).toBe(0);
    expect(AuditSeverity.Information).toBe(1);
    expect(AuditSeverity.Warning).toBe(2);
    expect(AuditSeverity.Error).toBe(3);
    expect(AuditSeverity.Critical).toBe(4);
  });

  it('LinkSourceType values', () => {
    expect(LinkSourceType.ExcelRange).toBe(0);
    expect(LinkSourceType.ExcelChart).toBe(1);
    expect(LinkSourceType.ExcelPivotTable).toBe(2);
  });

  it('LinkTargetType values', () => {
    expect(LinkTargetType.PowerPointShape).toBe(0);
    expect(LinkTargetType.PowerPointChart).toBe(1);
    expect(LinkTargetType.WordInlineShape).toBe(2);
    expect(LinkTargetType.WordTable).toBe(3);
  });
});