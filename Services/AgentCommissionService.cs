using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

public class AgentCommissionService
{
    private readonly ApplicationDbContext _context;

    public AgentCommissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> RecordPaymentAsync(
        int agentId,
        DateTime paymentDate,
        double amount,
        string? reference,
        string? notes,
        string recordedByUserId)
    {
        using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // Create payment header
            var payment = new tbl_CommissionPayments
            {
                sys_agentId = agentId,
                sys_paymentDate = paymentDate,
                sys_amountPaid = amount,
                sys_reference = reference,
                sys_notes = notes,
                sys_recordedByUserId = recordedByUserId
            };

            _context.tbl_CommissionPayments.Add(payment);

            await _context.SaveChangesAsync();

            double remaining = amount;

            // Load oldest outstanding commissions
            var commissions = await _context.tbl_CommissionsDue
                .Where(x =>
                    x.sys_agentId == agentId)
                .OrderBy(x => x.sys_commissionId)
                .ToListAsync();

            foreach (var commission in commissions)
            {
                var alreadyAllocated =
                    await _context.tbl_CommissionPaymentAllocations
                        .Where(x =>
                            x.sys_commissionId ==
                            commission.sys_commissionId)
                        .SumAsync(x =>
                            (double?)x.sys_amountAllocated) ?? 0;

                var outstanding =
                    commission.sys_amount -
                    alreadyAllocated;

                if (outstanding <= 0)
                    continue;

                var allocation =
                    Math.Min(
                        outstanding,
                        remaining);

                _context.tbl_CommissionPaymentAllocations.Add(
                    new tbl_CommissionPaymentAllocations
                    {
                        sys_commissionPaymentId =
                            payment.sys_commissionPaymentId,

                        sys_commissionId =
                            commission.sys_commissionId,

                        sys_amountAllocated =
                            allocation
                    });

                remaining -= allocation;

                if (remaining <= 0)
                    break;
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }


    public async Task<double> GetOutstandingAsync(int agentId)
    {
        var commissions =
            await _context.tbl_CommissionsDue
                .Where(x =>
                    x.sys_agentId == agentId)
                .ToListAsync();

        double outstanding = 0;

        foreach (var commission in commissions)
        {
            var paid =
                await _context.tbl_CommissionPaymentAllocations
                    .Where(x =>
                        x.sys_commissionId ==
                        commission.sys_commissionId)
                    .SumAsync(x =>
                        (double?)x.sys_amountAllocated) ?? 0;

            outstanding +=
                commission.sys_amount - paid;
        }

        return outstanding;
    }

    


}