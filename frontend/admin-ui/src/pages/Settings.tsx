import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { useToast } from '@/components/ui/use-toast';
import { AdminApi } from '@/api/psyAdmin';

const formSchema = z.object({
  currentPassword: z.string().min(1, {
    message: 'كلمة المرور الحالية مطلوبة',
  }),
  newPassword: z.string().min(8, {
    message: 'كلمة المرور الجديدة يجب أن تكون 8 أحرف على الأقل',
  }),
  confirmPassword: z.string().min(8, {
    message: 'تأكيد كلمة المرور مطلوب',
  }),
}).refine((data) => data.newPassword === data.confirmPassword, {
  message: "كلمتا المرور غير متطابقتين",
  path: ["confirmPassword"],
});

export default function Settings() {
  const [isLoading, setIsLoading] = useState(false);
  const { toast } = useToast();

  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      currentPassword: '',
      newPassword: '',
      confirmPassword: '',
    },
  });

  async function onSubmit(values: z.infer<typeof formSchema>) {
    setIsLoading(true);
    try {
      await AdminApi.changePassword(values.currentPassword, values.newPassword);
      toast({
        title: "تم تغيير كلمة المرور",
        description: "تم تغيير كلمة المرور بنجاح",
      });
      form.reset();
    } catch (error: any) {
      toast({
        title: "خطأ",
        description: error.response?.data?.error || "فشل تغيير كلمة المرور",
        variant: "destructive",
      });
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">الإعدادات</h1>
        <p className="text-gray-600">إدارة إعدادات حساب المشرف</p>
      </div>

      <Card className="max-w-md">
        <CardHeader>
          <CardTitle>تغيير كلمة المرور</CardTitle>
          <CardDescription>
            قم بتغيير كلمة المرور الخاصة بحساب المشرف
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
              <FormField
                control={form.control}
                name="currentPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>كلمة المرور الحالية</FormLabel>
                    <FormControl>
                      <Input type="password" placeholder="أدخل كلمة المرور الحالية" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="newPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>كلمة المرور الجديدة</FormLabel>
                    <FormControl>
                      <Input type="password" placeholder="أدخل كلمة المرور الجديدة" {...field} />
                    </FormControl>
                    <FormDescription>
                      يجب أن تكون كلمة المرور 8 أحرف على الأقل
                    </FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="confirmPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>تأكيد كلمة المرور الجديدة</FormLabel>
                    <FormControl>
                      <Input type="password" placeholder="أعد إدخال كلمة المرور الجديدة" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <Button type="submit" disabled={isLoading} className="w-full">
                {isLoading ? 'جاري الحفظ...' : 'حفظ التغييرات'}
              </Button>
            </form>
          </Form>
        </CardContent>
      </Card>
    </div>
  );
}
